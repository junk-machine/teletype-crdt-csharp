using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Teletype.Contracts;
using Teletype.Properties;

namespace Teletype
{
    /// <summary>
    /// CRDT structure for Teletype document.
    /// </summary>
    public class Document
    {
        /// <summary>
        /// Text segments tree that holds entire document.
        /// </summary>
        private readonly DocumentTree documentTree;

        /// <summary>
        /// Mapping between splice identifier and associated <see cref="SplitTree"/> structure.
        /// </summary>
        private readonly Dictionary<SpliceId, SplitTree> splitTreesBySpliceId;

        /// <summary>
        /// Mapping between splice identifier and associated text deletion modification, if any.
        /// </summary>
        private readonly Dictionary<SpliceId, TextDeletionModification> deletionsBySpliceId;

        /// <summary>
        /// Mapping between splice identifier and number of undo operations for tha splice.
        /// </summary>
        private Dictionary<SpliceId, int> undoCountsBySpliceId;

        /// <summary>
        /// Mapping between site identifier, layer ID and marker ID and associated <see cref="Marker{T}"/> structure.
        /// </summary>
        private readonly Dictionary<int, Dictionary<int, Dictionary<int, Marker<LogicalRange>>>> markerLayersBySiteId;

        /// <summary>
        /// Mapping between splice identifier and all deferred operations that depend on this splice.
        /// </summary>
        private readonly Dictionary<SpliceId, IList<IOperation>> deferredOperationsByDependencyId;

        /// <summary>
        /// Mapping between site identifier, markers layer identifier, marker identifier and actual marker.
        /// </summary>
        private readonly Dictionary<int, Dictionary<int, Dictionary<int, Marker<LogicalRange>>>> deferredMarkerUpdates;

        /// <summary>
        /// Mapping between splice identifier and all deferred marker updates that depend on this splice. 
        /// </summary>
        private readonly Dictionary<SpliceId, IList<DeferredMarkerUpdateOperation>> deferredMarkerUpdatesByDependencyId;

        /// <summary>
        /// Mapping between site identifier and last sequence number for that site.
        /// </summary>
        private readonly Dictionary<int, int> maxSeqsBySite;

        /// <summary>
        /// All text operations for the document.
        /// </summary>
        private readonly List<IOperation> operations;

        /// <summary>
        /// Next sequence number for the current site.
        /// </summary>
        private int nextSequenceNumber = 1;

        /// <summary>
        /// Next checkpoint identifier.
        /// </summary>
        private int nextCheckpointId = 1;

        /// <summary>
        /// Undo operations stack.
        /// </summary>
        private Stack<IUndoRecord> undoStack;

        /// <summary>
        /// Redo operations stack.
        /// </summary>
        private Stack<IUndoRecord> redoStack;

        /// <summary>
        /// Gets the identifier of the current site.
        /// </summary>
        public int SiteId { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="Document"/> class
        /// with the provided site identifier.
        /// </summary>
        /// <param name="siteId">Current site identifier</param>
        public Document(int siteId)
        {
            if (siteId == 0)
            {
                throw new ArgumentOutOfRangeException("siteId", string.Format(Resources.ReservedSiteIdErrorFormat, siteId));
            }

            SiteId = siteId;

            splitTreesBySpliceId = new Dictionary<SpliceId, SplitTree>();

            var firstSegment = new Segment(new SpliceId(0, 0), Point.Zero, string.Empty, Point.Zero);
            splitTreesBySpliceId.Add(firstSegment.SpliceId, new SplitTree(firstSegment));

            var lastSegment = new Segment(new SpliceId(0, 1), Point.Zero, string.Empty, Point.Zero);
            splitTreesBySpliceId.Add(lastSegment.SpliceId, new SplitTree(lastSegment));

            documentTree = new DocumentTree(firstSegment, lastSegment, IsSegmentVisible);

            deletionsBySpliceId = new Dictionary<SpliceId, TextDeletionModification>();

            undoCountsBySpliceId = new Dictionary<SpliceId, int>();

            markerLayersBySiteId = new Dictionary<int, Dictionary<int, Dictionary<int, Marker<LogicalRange>>>>();
            markerLayersBySiteId.Add(siteId, new Dictionary<int, Dictionary<int, Marker<LogicalRange>>>());

            deferredOperationsByDependencyId = new Dictionary<SpliceId, IList<IOperation>>();
            deferredMarkerUpdates = new Dictionary<int, Dictionary<int, Dictionary<int, Marker<LogicalRange>>>>();
            deferredMarkerUpdatesByDependencyId = new Dictionary<SpliceId, IList<DeferredMarkerUpdateOperation>>();

            maxSeqsBySite = new Dictionary<int, int>();

            operations = new List<IOperation>();

            undoStack = new Stack<IUndoRecord>();
            redoStack = new Stack<IUndoRecord>();
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Document"/> class
        /// with the provided site identifier and text.
        /// </summary>
        /// <param name="siteId">Current site identifier</param>
        /// <param name="text">Current text of the document</param>
        public Document(int siteId, string text)
            : this(siteId)
        {
            if (!string.IsNullOrEmpty(text))
            {
                SetTextInRange(Point.Zero, Point.Zero, text);

                // Do not allow to undo initial text
                ClearUndoStack();
            }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Document"/> class
        /// with the provided site identifier and history.
        /// </summary>
        /// <param name="siteId">Current site identifier</param>
        /// <param name="history">History of the edits</param>
        public Document(int siteId, History history)
            : this(siteId)
        {
            if (history != null)
            {
                PopulateHistory(history);
            }
        }

        /// <summary>
        /// Gets the text of the entire document.
        /// </summary>
        /// <returns>Text of the document.</returns>
        public string GetText()
        {
            var text = new StringBuilder();

            foreach (var segment in documentTree.GetSegments())
            {
                if (IsSegmentVisible(segment, null, null))
                {
                    text.Append(segment.Text);
                }
            }

            return text.ToString();
        }

        /// <summary>
        /// Finds two segments so that given <paramref name="position"/> falls between them.
        /// If position falls within one single segment, then it gets split.
        /// </summary>
        /// <param name="position">Look-up position</param>
        /// <returns>Left and right segments around the given position.</returns>
        private Tuple<Segment, Segment> FindLocalSegmentBoundary(Point position)
        {
            ( var segment, var start, var end ) = documentTree.FindSegmentContainingPosition(position);

            if (position.CompareTo(end) < 0)
            {
                var splitTree = splitTreesBySpliceId[segment.SpliceId];
                return SplitSegment(splitTree, segment, Point.Traversal(position, start));
            }
            else
            {
                return Tuple.Create(segment, documentTree.GetSuccessor(segment));
            }
        }

        /// <summary>
        /// Splits the segment within the given split tree.
        /// </summary>
        /// <param name="splitTree">Parent split tree where the segment should be split</param>
        /// <param name="segment">Segment to split</param>
        /// <param name="offset">Split offset</param>
        /// <returns>Two segments that are result of the split operation</returns>
        private Tuple<Segment, Segment> SplitSegment(SplitTree splitTree, Segment segment, Point offset)
        {
            var suffix = splitTree.SplitSegment(segment, offset);
            documentTree.SplitSegment(segment, suffix);
            return Tuple.Create(segment, suffix);
        }

        /// <summary>
        /// Finds the segment where given absolute <paramref name="position"/> falls into.
        /// </summary>
        /// <param name="position">Absolute text position</param>
        /// <param name="preferStart">Whether to return the next segment, when position falls in-between two segments</param>
        /// <returns>Found segment and text position.</returns>
        private Tuple<Segment, Point> FindSegment(Point position, bool preferStart)
        {
            (var segment, var start, var end) = documentTree.FindSegmentContainingPosition(position);

            var offset = segment.Offset.Traverse(Point.Traversal(position, start));

            if (preferStart && position.CompareTo(end) == 0)
            {
                segment = documentTree.GetSuccessor(segment);
                offset = segment.Offset;
            }

            return Tuple.Create(segment, offset);
        }

        /// <summary>
        /// Finds the segment for which the given text <paramref name="offset"/> is a starting position.
        /// If given <paramref name="offset"/> falls in the middle of the segment,
        /// then said segment is being split in two and suffix (right segment) is returned.
        /// </summary>
        /// <param name="spliceId">Identifier of the splice</param>
        /// <param name="offset">Text offset</param>
        /// <returns>Segment that starts at the offset.</returns>
        private Segment FindSegmentStart(SpliceId spliceId, Point offset)
        {
            var splitTree = splitTreesBySpliceId[spliceId];
            var segment = splitTree.FindSegmentContainingOffset(offset);
            var segmentEndOffset = segment.Offset.Traverse(segment.Extent);

            if (segment.Offset.CompareTo(offset) == 0)
            {
                return segment;
            }
            else if (segmentEndOffset.CompareTo(offset) == 0)
            {
                return segment.NextSplit;
            }
            else
            {
                (var _, var suffix) = SplitSegment(splitTree, segment, Point.Traversal(offset, segment.Offset));
                return suffix;
            }
        }

        /// <summary>
        /// Finds the segment for which the given text <paramref name="offset"/> is an end position.
        /// If given <paramref name="offset"/> falls in the middle of the segment,
        /// then said segment is being split in two and prefix (left segment) is returned.
        /// </summary>
        /// <param name="spliceId">Identifier of the splice</param>
        /// <param name="offset">Text offset</param>
        /// <returns>Segment that ends at the offset.</returns>
        private Segment FindSegmentEnd(SpliceId spliceId, Point offset)
        {
            var splitTree = splitTreesBySpliceId[spliceId];
            var segment = splitTree.FindSegmentContainingOffset(offset);
            var segmentEndOffset = segment.Offset.Traverse(segment.Extent);

            if (segmentEndOffset.CompareTo(offset) == 0)
            {
                return segment;
            }
            else
            {
                (var prefix, var _) = SplitSegment(splitTree, segment, Point.Traversal(offset, segment.Offset));
                return prefix;
            }
        }

        /// <summary>
        /// Replaces existing text in the given range with the new text.
        /// </summary>
        /// <param name="start">Start of the range</param>
        /// <param name="end">End of the range</param>
        /// <param name="text">New text</param>
        /// <returns>Integrated operation.</returns>
        /// <remarks>
        /// The <paramref name="start"/> position can be the same as <paramref name="end"/>,
        /// in which case new text is inserted in that location.
        /// </remarks>
        public SpliceOperation SetTextInRange(Point start, Point end, string text)
        {
            var spliceId = new SpliceId(SiteId, nextSequenceNumber);
            TextDeletionModification deletion = null;
            TextInsertionModification insertion = null;

            if (end.CompareTo(start) > 0)
            {
                deletion = Delete(spliceId, start, end);
            }

            if (!string.IsNullOrEmpty(text))
            {
                insertion = Insert(spliceId, start, text);
            }

            UpdateMaxSeqsBySite(spliceId);

            var operation = new SpliceOperation(spliceId, deletion, insertion);

            undoStack.Push(new TransactionRecord(GetNow(), new List<IOperation> { operation }));
            ClearRedoStack();
            
            operations.Add(operation);
            return operation;
        }

        /// <summary>
        /// Retrieves all markers currently available on this <see cref="Document"/>.
        /// </summary>
        /// <returns>Mapping between site identifier, marker layer identifier, marker identifier and the marker.</returns>
        public IReadOnlyDictionary<int, IReadOnlyDictionary<int, IReadOnlyDictionary<int, Marker<Range>>>> GetMarkers()
        {
            var result = new Dictionary<int, IReadOnlyDictionary<int, IReadOnlyDictionary<int, Marker<Range>>>>(markerLayersBySiteId.Count);

            foreach (var layersForSite in markerLayersBySiteId)
            {
                if (layersForSite.Value.Count > 0)
                {
                    var markersByLayerId = new Dictionary<int, IReadOnlyDictionary<int, Marker<Range>>>(layersForSite.Value.Count);
                    result[layersForSite.Key] = markersByLayerId;

                    foreach (var markersForLayer in layersForSite.Value)
                    {
                        if (markersForLayer.Value.Count > 0)
                        {
                            var markers = new Dictionary<int, Marker<Range>>();
                            markersByLayerId[markersForLayer.Key] = markers;

                            foreach (var marker in markersForLayer.Value)
                            {
                                markers[marker.Key] =
                                    new Marker<Range>(
                                        marker.Value.Exclusive,
                                        marker.Value.Reversed,
                                        marker.Value.Tailed,
                                        ResolveLogicalRange(marker.Value.Range, marker.Value.Exclusive));
                            }
                        }
                    }
                }
            }

            return result;
        }

        /// <summary>
        /// Updates markers in the Document.
        /// If a marker exists with the specified ID, then its value is updated.
        /// If no marker exists with the specified ID, then new one is created.
        /// </summary>
        /// <param name="layerUpdatesById">Markers updates by marker layer identifier for the current site.</param>
        /// <returns>Operation that defines updates of document markers for the current site.</returns>
        public MarkersUpdateOperation UpdateMarkers(Dictionary<int, Dictionary<int, Marker<Range>>> layerUpdatesById)
        {
            var updates = new Dictionary<int, IReadOnlyDictionary<int, Marker<LogicalRange>>>();

            var layers = markerLayersBySiteId[SiteId];

            foreach (var markersForLayer in layerUpdatesById)
            {
                var layerUpdate = markersForLayer.Value;
                var layerId = markersForLayer.Key;

                layers.TryGetValue(layerId, out var layer);

                if (layerUpdate == null)
                {
                    if (layer != null)
                    {
                        layers.Remove(layerId);
                        updates.Add(layerId, null);
                    }
                }
                else
                {
                    if (layer == null)
                    {
                        layer = new Dictionary<int, Marker<LogicalRange>>();
                        layers.Add(layerId, layer);
                    }

                    var updatesForLayer = new Dictionary<int, Marker<LogicalRange>>();
                    updates.Add(layerId, updatesForLayer);

                    foreach (var markerWithId in layerUpdate) {
                        var markerUpdate = markerWithId.Value;
                        layer.TryGetValue(markerWithId.Key, out var marker);

                        if (markerUpdate != null)
                        {
                            var existingMarkerExclusive = false;
                            LogicalRange existingMarkerRange = null;

                            if (marker != null)
                            {
                                existingMarkerExclusive = marker.Exclusive;
                                existingMarkerRange = marker.Range;
                            }

                            if (markerUpdate.Range != null || existingMarkerExclusive != markerUpdate.Exclusive)
                            {
                                existingMarkerRange =
                                    GetLogicalRange(
                                        markerUpdate.Range
                                            // If updating exclusive flag only (no range provided) - reuse current marker range
                                            ?? ResolveLogicalRange(existingMarkerRange, existingMarkerExclusive),
                                        markerUpdate.Exclusive);
                            }

                            // TODO: Original JavaScript implementation allows to have partial updates - no value for one or more flags.
                            //       We currently use non-nullable booleans, so all values have to be provided from other clients,
                            //       otherwise they will reset to defaults. This is better to be handled in serialization to/from JavaScript.
                            marker =
                                new Marker<LogicalRange>(
                                    markerUpdate.Exclusive,
                                    markerUpdate.Reversed,
                                    markerUpdate.Tailed,
                                    existingMarkerRange);
                            
                            layer[markerWithId.Key] = marker;
                            updatesForLayer.Add(markerWithId.Key, marker);
                        }
                        else
                        {
                            layer.Remove(markerWithId.Key);
                            updatesForLayer.Add(markerWithId.Key, null);
                        }
                    }
                }
            }

            return new MarkersUpdateOperation(SiteId, updates);
        }

        /// <summary>
        /// Undoes the latest transaction on the undo stack.
        /// If there's a barrier before the latest transaction, then nothing happens.
        /// </summary>
        /// <returns>Undo operations with associated text and marker changes or null.</returns>
        public UndoRedoResults Undo()
        {
            var spliceIndex = 0;
            var transactionFound = false;
            IEnumerable<IOperation> operationsToUndo = null;
            IReadOnlyDictionary<int, IReadOnlyDictionary<int, Marker<LogicalRange>>> markersSnapshot = null;

            foreach (var stackEntry in undoStack) 
            {
                ++spliceIndex;

                if (stackEntry is TransactionRecord transaction)
                {
                    operationsToUndo = transaction.Operations;
                    markersSnapshot = transaction.MarkersSnapshotBefore;
                    transactionFound = true;
                    break;
                }
                else if (stackEntry is CheckpointRecord checkpoint && checkpoint.IsBarrier)
                {
                    return null;
                }
            }

            if (transactionFound)
            {
                // Move all preceeding checkpoints and actual transaction to the redo stack
                while (spliceIndex-- > 0)
                {
                    redoStack.Push(undoStack.Pop());
                }
                
                var (operations, textUpdates) = UndoOrRedoOperations(operationsToUndo);
                return new UndoRedoResults(
                    operations,
                    textUpdates,
                    MarkersFromSnapshot(markersSnapshot));
            }
            else
            {
                return null;
            }
        }

        /// <summary>
        /// Redoes the latest transaction on the redo stack.
        /// </summary>
        /// <returns>Redo operations with associated text and marker changes or null.</returns>
        public UndoRedoResults Redo()
        {
            int spliceIndex = 0;
            var transactionFound = false;
            IEnumerable<IOperation> operationsToRedo = null;
            IReadOnlyDictionary<int, IReadOnlyDictionary<int, Marker<LogicalRange>>> markersSnapshot = null;

            foreach (var stackEntry in redoStack)
            {
                spliceIndex++;

                if (stackEntry is TransactionRecord transaction)
                {
                    operationsToRedo = transaction.Operations;
                    markersSnapshot = transaction.MarkersSnapshotAfter;
                    transactionFound = true;
                    break;
                }
            }

            if (transactionFound)
            {
                // Move all preceeding checkpoints and actual transaction to the undo stack
                while (spliceIndex-- > 0)
                {
                    undoStack.Push(redoStack.Pop());
                }

                // Move all following checkpoints to the undo stack
                while (redoStack.Count > 0 && redoStack.Peek() is CheckpointRecord)
                {
                    undoStack.Push(redoStack.Pop());
                }

                var (operations, textUpdates) = UndoOrRedoOperations(operationsToRedo);
                return new UndoRedoResults(
                    operations,
                    textUpdates,
                    markersSnapshot != null ? MarkersFromSnapshot(markersSnapshot) : null);
            }
            else
            {
                return null;
            }
        }

        /// <summary>
        /// Empties the undo stack.
        /// </summary>
        public void ClearUndoStack()
        {
            undoStack.Clear();
        }

        /// <summary>
        /// Empties the redo stack.
        /// </summary>
        public void ClearRedoStack()
        {
            redoStack.Clear();
        }

        /// <summary>
        /// Groups together transactions that happened within the specified <paramref name="groupingInterval"/>.
        /// </summary>
        /// <param name="groupingInterval">Interval in milliseconds</param>
        public void ApplyGroupingInterval(int groupingInterval)
        {
            var topEntry = undoStack.Count > 0 ? undoStack.Peek() : null;
            var previousEntry = undoStack.Skip(1).FirstOrDefault();

            if (topEntry is TransactionRecord topTransaction)
            {
                topTransaction.GroupingInterval = groupingInterval;
            }
            else
            {
                return;
            }

            if (previousEntry is TransactionRecord previousTransaction)
            {
                var timeBetweenEntries = topTransaction.Timestamp - previousTransaction.Timestamp;
                var minGroupingInterval = Math.Min(groupingInterval, previousTransaction.GroupingInterval ?? long.MaxValue);

                if (timeBetweenEntries < minGroupingInterval)
                {
                    undoStack.Pop();
                    previousTransaction.Timestamp = topTransaction.Timestamp;
                    previousTransaction.GroupingInterval = groupingInterval;
                    previousTransaction.Operations.AddRange(topTransaction.Operations);
                    previousTransaction.MarkersSnapshotAfter = topTransaction.MarkersSnapshotAfter;
                }
            }
        }

        /// <summary>
        /// Creates a Checkpoint in the undo stack.
        /// If a checkpoint is a barrier, transactions chronologically before it cannot be
        /// undone or grouped with Transactions before it.
        /// </summary>
        /// <param name="isBarrier">
        /// Flag that indicates whether to create a barrier checkpoint.
        /// Transactions cannot be grouped over the barrier.
        /// </param>
        /// <param name="markers">Markers state</param>
        /// <returns>Identifier of the newly created checkpoint.</returns>
        public int CreateCheckpoint(bool isBarrier = false, IReadOnlyDictionary<int, IReadOnlyDictionary<int, Marker<Range>>> markers = null)
        {
            var checkpoint =
                new CheckpointRecord(
                  nextCheckpointId++,
                  isBarrier,
                  markers != null ? SnapshotFromMarkers(markers) : null);

            undoStack.Push(checkpoint);
            return checkpoint.Id;
        }

        /// <summary>
        /// Checks if a barrier checkpoint is present chronologically before a given checkpoint.
        /// This is used to prevent undo operations or transaction grouping over barriers.
        /// </summary>
        /// <param name="checkpointId">Identifier of the checkpoint to analyze upto</param>
        /// <returns>true if barrier is present, otherwise false.</returns>
        private bool IsBarrierPresentBeforeCheckpoint(int checkpointId)
        {
            foreach (var stackEntry in undoStack)
            {
                if (stackEntry is CheckpointRecord checkpoint) {
                    if (checkpoint.Id == checkpointId)
                    {
                        return false;
                    }

                    if (checkpoint.IsBarrier)
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        /// <summary>
        /// Groups changes that happened after the checkpoint with given identifier.
        /// </summary>
        /// <param name="checkpointId">Identifier of the checkpoint that serves as a reference point</param>
        /// <param name="deleteCheckpoint">Flag that indicates whether checkpoint should be deleted after grouping</param>
        /// <param name="markers">Markers state</param>
        /// <returns>Text updates for all grouped operations or null.</returns>
        public IEnumerable<TextUpdate> GroupChangesSinceCheckpoint(
            int checkpointId,
            bool deleteCheckpoint = false,
            IReadOnlyDictionary<int, IReadOnlyDictionary<int, Marker<Range>>> markers = null)
        {
            if (IsBarrierPresentBeforeCheckpoint(checkpointId))
            {
                return null;
            }

            var result = CollectOperationsSinceCheckpoint(checkpointId, true, deleteCheckpoint);

            if (result != null)
            {
                var ( operations, markersSnapshot ) = result;

                if (operations.Count > 0)
                {
                    undoStack.Push(new TransactionRecord(
                        GetNow(),
                        operations,
                        markersSnapshot,
                        markers != null ? SnapshotFromMarkers(markers) : null));

                    return TextUpdatesForOperations(operations, null);
                }
                else
                {
                    return Enumerable.Empty<TextUpdate>();
                }
            }
            else
            {
                return null;
            }
        }

        /// <summary>
        /// Reverts the document to a checkpoint on the undo stack.
        /// If a barrier exists on the undo stack before the checkpoint matching
        /// <paramref name="checkpointId"/>, the reversion fails.
        /// </summary>
        /// <param name="checkpointId">Identifier of the checkpoint to revert to</param>
        /// <param name="deleteCheckpoint">Falg that indicates whether checkpoint should be deleted</param>
        /// <returns>Results of the undo operation or null.</returns>
        public UndoRedoResults RevertToCheckpoint(int checkpointId, bool deleteCheckpoint = false)
        {
            if (IsBarrierPresentBeforeCheckpoint(checkpointId))
            {
                return null;
            }

            var collectResult = CollectOperationsSinceCheckpoint(checkpointId, true, deleteCheckpoint);

            if (collectResult != null)
            {
                var (operations, textUpdates) = UndoOrRedoOperations(collectResult.Item1);
                return new UndoRedoResults(
                    operations,
                    textUpdates,
                    MarkersFromSnapshot(collectResult.Item2));
            }
            else
            {
                return null;
            }
        }

        /// <summary>
        /// Gets changes performed since a checkpoint.
        /// </summary>
        /// <param name="checkpointId">Identifier of the checkpoint</param>
        /// <returns>Text changes or null.</returns>
        public IEnumerable<TextUpdate> GetChangesSinceCheckpoint(int checkpointId)
        {
            var result = CollectOperationsSinceCheckpoint(checkpointId, false, false);
            return result != null
                ? TextUpdatesForOperations(result.Item1, null)
                : null;
        }

        /// <summary>
        /// Collects all operations that happened since the given checkpoint.
        /// </summary>
        /// <param name="checkpointId">Identifier of the checkpoint that serves as a reference point</param>
        /// <param name="deleteOperations">Flag that indicates whether operations should be deleted from the undo stack, after they are collected</param>
        /// <param name="deleteCheckpoint">Flag that indicates whether checkpoint record itself should be deleted</param>
        /// <returns>List of operations and markers state or null.</returns>
        private Tuple<List<IOperation>, IReadOnlyDictionary<int, IReadOnlyDictionary<int, Marker<LogicalRange>>>> CollectOperationsSinceCheckpoint(
            int checkpointId,
            bool deleteOperations,
            bool deleteCheckpoint)
        {
            var checkpointFound = false;
            var checkpointIndex = 0;
            IReadOnlyDictionary<int, IReadOnlyDictionary<int, Marker<LogicalRange>>> markersSnapshot = null;
            var operations = new List<IOperation>();

            foreach (var stackEntry in undoStack)
            {
                ++checkpointIndex;
                if (stackEntry is CheckpointRecord checkpoint)
                {
                    if (checkpoint.Id == checkpointId)
                    {
                        checkpointFound = true;
                        markersSnapshot = checkpoint.MarkersSnapshot;
                        break;
                    }
                }
                else if (stackEntry is TransactionRecord transaction)
                {
                    operations.AddRange(transaction.Operations);
                }
                else
                {
                    throw new InvalidOperationException(string.Format(Resources.UnknownUndoStackEntryErrorFormat, stackEntry.GetType()));
                }
            }

            if (checkpointFound)
            {
                if (deleteOperations)
                {
                    if (!deleteCheckpoint)
                    {
                        --checkpointIndex;
                    }

                    while (checkpointIndex-- > 0)
                    {
                        undoStack.Pop();
                    }
                }

                return Tuple.Create(operations, markersSnapshot);
            }

            return null;
        }

        /// <summary>
        /// Groups the last two changes on the undo stack.
        /// </summary>
        /// <returns>true if a grouping was made; otherwise false.</returns>
        public bool GroupLastChanges()
        {
            TransactionRecord lastTransaction = null;
            int numberOfRecords = 0;

            foreach (var stackEntry in undoStack)
            {
                ++numberOfRecords;

                if (stackEntry is CheckpointRecord checkpoint)
                {
                    if (checkpoint.IsBarrier)
                    {
                        return false;
                    }
                }
                else if (stackEntry is TransactionRecord transaction)
                {
                    if (lastTransaction != null)
                    {
                        // We are going to return anyway, so can modify the collection here.
                        // Remove everything except the current transaction, as it will be updated in-place
                        // to include operations from lastTransaction.
                        while (--numberOfRecords > 0)
                        {
                            undoStack.Pop();
                        }

                        transaction.Timestamp = GetNow();
                        transaction.GroupingInterval = null;
                        transaction.Operations.AddRange(lastTransaction.Operations);
                        transaction.MarkersSnapshotAfter = lastTransaction.MarkersSnapshotAfter;

                        return true;
                    }
                    else
                    {
                        lastTransaction = transaction;
                    }
                }
            }

            return false;
        }

        /// <summary>
        ///  Gets a serializable representation of the history.
        /// </summary>
        /// <param name="maxEntries">Maximum number of history entries to return</param>
        /// <returns>The document history.</returns>
        public History GetHistory(int maxEntries)
        {
            var originalUndoCounts = new Dictionary<SpliceId, int>(undoCountsBySpliceId);

            var redoStack = new IUndoHistoryRecord[Math.Min(this.redoStack.Count, maxEntries)];
            var recordIndex = redoStack.Length;

            foreach (var entry in this.redoStack)
            {
                if (--recordIndex < 0)
                {
                    break;
                }

                if (entry is TransactionRecord transaction)
                {
                    // Do not reorder these calls! UndoOrRedoOperations(..) updates the document state
                    // and it affects the result of MarkersFromSnapshot(..)
                    var markersBefore = MarkersFromSnapshot(transaction.MarkersSnapshotBefore);
                    var changes = UndoOrRedoOperations(transaction.Operations).Item2;
                    var markersAfter = MarkersFromSnapshot(transaction.MarkersSnapshotAfter);

                    redoStack[recordIndex] =
                        new TransactionHistoryRecord(changes, markersBefore, markersAfter);
                }
                else if (entry is CheckpointRecord checkpoint)
                {
                    redoStack[recordIndex] =
                        new CheckpointHistoryRecord(
                            checkpoint.Id,
                            MarkersFromSnapshot(checkpoint.MarkersSnapshot));
                }
            }

            // Undo operations we redid above while computing changes
            recordIndex = 0;
            foreach (var entry in this.redoStack)
            {
                if (recordIndex++ >= redoStack.Length)
                {
                    break;
                }

                if (entry is TransactionRecord transaction)
                {
                    UndoOrRedoOperations(transaction.Operations);
                }
            }

            var undoStack = new IUndoHistoryRecord[Math.Min(this.undoStack.Count, maxEntries)];
            recordIndex = undoStack.Length;

            foreach (var entry in this.undoStack)
            {
                if (--recordIndex < 0)
                {
                    break;
                }

                if (entry is TransactionRecord transaction) {
                    // Do not reorder these calls! UndoOrRedoOperations(..) updates the document state
                    // and it affects the result of MarkersFromSnapshot(..)
                    var markersAfter = MarkersFromSnapshot(transaction.MarkersSnapshotAfter);
                    var changes = InvertTextUpdates(UndoOrRedoOperations(transaction.Operations).Item2);
                    var markersBefore = MarkersFromSnapshot(transaction.MarkersSnapshotBefore);

                    undoStack[recordIndex] =
                        new TransactionHistoryRecord(changes, markersBefore, markersAfter);
                }
                else if (entry is CheckpointRecord checkpoint)
                {
                    undoStack[recordIndex] =
                        new CheckpointHistoryRecord(
                            checkpoint.Id,
                            MarkersFromSnapshot(checkpoint.MarkersSnapshot));
                }
            }

            // Redo operations we undid above while computing changes
            recordIndex = 0;
            foreach (var entry in this.undoStack)
            {
                if (recordIndex++ >= undoStack.Length)
                {
                    break;
                }

                if (entry is TransactionRecord transaction)
                {
                    UndoOrRedoOperations(transaction.Operations);
                }
            }

            undoCountsBySpliceId = originalUndoCounts;

            return
                new History(
                    null,
                    nextCheckpointId,
                    undoStack,
                    redoStack);
        }

        /// <summary>
        /// Deletes text fragment between <paramref name="start"/> and <paramref name="end"/> and
        /// returns associated <see cref="TextDeletionModification"/>.
        /// </summary>
        /// <param name="spliceId">Identifier of the splice</param>
        /// <param name="start">Text location to start deletion at</param>
        /// <param name="end">Text location to end deletion at</param>
        /// <returns>Text deletion modification that represents the deletion.</returns>
        private TextDeletionModification Delete(SpliceId spliceId, Point start, Point end)
        {
            (var _, var left) = FindLocalSegmentBoundary(start);
            (var right, var _) = FindLocalSegmentBoundary(end);

            var maxSeqsBySite = new Dictionary<int, int>();
            var segment = left;
            while (true)
            {
                if (!maxSeqsBySite.TryGetValue(segment.SpliceId.SiteId, out int maxSeq)
                    || segment.SpliceId.SequenceNumber > maxSeq)
                {
                    maxSeqsBySite[segment.SpliceId.SiteId] = segment.SpliceId.SequenceNumber;
                }

                segment.Deletions.Add(spliceId);
                documentTree.SplayNode(segment);
                documentTree.UpdateSubtreeExtent(segment);

                if (segment == right)
                {
                    break;
                }

                segment = documentTree.GetSuccessor(segment);
            }

            var deletion =
                new TextDeletionModification(
                    spliceId,
                    maxSeqsBySite,
                    left.SpliceId,
                    left.Offset,
                    right.SpliceId,
                    right.Offset.Traverse(right.Extent));

            deletionsBySpliceId[spliceId] = deletion;

            return deletion;
        }

        /// <summary>
        /// Inserts given <paramref name="text"/> at the given <paramref name="position"/> and
        /// returns associated <see cref="TextInsertionModification"/>.
        /// </summary>
        /// <param name="spliceId">Identifier of the splice</param>
        /// <param name="position">Location to insert the text</param>
        /// <param name="text">Text to insert</param>
        /// <returns>Text insertion modification that represents the insertion.</returns>
        private TextInsertionModification Insert(SpliceId spliceId, Point position, string text)
        {
            (var left, var right) = FindLocalSegmentBoundary(position);
            var newSegment = new Segment(spliceId, Point.Zero, text, Point.GetExtentForText(text));
            newSegment.LeftDependency = left;
            newSegment.RightDependency = right;

            documentTree.InsertBetween(left, right, newSegment);
            splitTreesBySpliceId[spliceId] = new SplitTree(newSegment);

            return new TextInsertionModification(
              text,
              left.SpliceId,
              left.Offset.Traverse(left.Extent),
              right.SpliceId,
              right.Offset);
        }


        /// <summary>
        /// Determines whether given <paramref name="segment"/> is visible within the document.
        /// </summary>
        /// <param name="segment">Document segment</param>
        /// <param name="undoCountOverrides">Overrides for number of undo operations per splice</param>
        /// <param name="operationsToIgnore">Identifiers of splices for operations that should be ignored</param>
        /// <returns>true if segment is visible, otherwise false.</returns>
        private bool IsSegmentVisible(
            Segment segment,
            Dictionary<SpliceId, int> undoCountOverrides,
            HashSet<SpliceId> operationsToIgnore)
        {
            if (operationsToIgnore != null && operationsToIgnore.Contains(segment.SpliceId))
            {
                return false;
            }

            int undoCount;
            if (undoCountOverrides == null
                || !undoCountOverrides.TryGetValue(segment.SpliceId, out undoCount))
            {
                undoCountsBySpliceId.TryGetValue(segment.SpliceId, out undoCount);
            }

            return
              (undoCount & 1) == 0 &&
              !IsSegmentDeleted(segment, undoCountOverrides, operationsToIgnore);
        }

        /// <summary>
        /// Determines whether given <paramref name="segment"/> was deleted.
        /// </summary>
        /// <param name="segment">Document segment</param>
        /// <param name="undoCountOverrides">Overrides for number of undo operations per splice</param>
        /// <param name="operationsToIgnore">Identifiers of splices for operations that should be ignored</param>
        /// <returns>true if segment was deleted, otherwise false.</returns>
        private bool IsSegmentDeleted(
            Segment segment,
            Dictionary<SpliceId, int> undoCountOverrides,
            HashSet<SpliceId> operationsToIgnore)
        {
            foreach (var deletionSpliceId in segment.Deletions)
            {
                if (operationsToIgnore != null && operationsToIgnore.Contains(deletionSpliceId))
                {
                    continue;
                }

                int deletionUndoCount;
                if (undoCountOverrides == null
                    || !undoCountOverrides.TryGetValue(deletionSpliceId, out deletionUndoCount))
                {
                    undoCountsBySpliceId.TryGetValue(deletionSpliceId, out deletionUndoCount);
                }

                if ((deletionUndoCount & 1) == 0)
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Updates last sequence number for a given site, based on the provided <paramref name="spliceId"/>.
        /// </summary>
        /// <param name="spliceId">Identifier of the last splice for a given site</param>
        private void UpdateMaxSeqsBySite(SpliceId spliceId)
        {
            if (!maxSeqsBySite.TryGetValue(spliceId.SiteId, out int previousSeq))
            {
                previousSeq = 0;
            }

            if (previousSeq != spliceId.SequenceNumber - 1)
            {
                throw new InvalidOperationException(string.Format(Resources.OutOfOrderOperationsErrorFormat, spliceId.SiteId));
            }
            
            maxSeqsBySite[spliceId.SiteId] = spliceId.SequenceNumber;

            if (SiteId == spliceId.SiteId)
            {
                nextSequenceNumber = spliceId.SequenceNumber + 1;
            }
        }

        /// <summary>
        /// Creates a copy of this document.
        /// </summary>
        /// <returns>Copy of this document.</returns>
        public Document Replicate()
        {
            var replica = new Document(SiteId);
            replica.IntegrateOperations(GetOperations().ToList());
            return replica;
        }

        /// <summary>
        /// Reverts changes done by the given set of operations.
        /// </summary>
        /// <param name="operationsToUndo">Operations to undo</param>
        /// <returns>Collection of counter-operations and associated text modifications.</returns>
        protected Tuple<IReadOnlyList<UndoOperation>, IReadOnlyList<TextUpdate>> UndoOrRedoOperations(IEnumerable<IOperation> operationsToUndo)
        {
            var undoOperations = new List<UndoOperation>();
            var oldUndoCounts = new Dictionary<SpliceId, int>();

            foreach (var operationToUndo in operationsToUndo)
            {
                if (operationToUndo is IModificationOperation modificationOperation)
                {
                    var spliceId = modificationOperation.SpliceId;

                    undoCountsBySpliceId.TryGetValue(spliceId, out var newUndoCount);
                    newUndoCount += 1;
                    UpdateUndoCount(spliceId, newUndoCount, oldUndoCounts);

                    var operation = new UndoOperation(spliceId, newUndoCount);
                    undoOperations.Add(operation);
                    operations.Add(operation);
                }
            }

            return new Tuple<IReadOnlyList<UndoOperation>, IReadOnlyList<TextUpdate>>(
                undoOperations,
                TextUpdatesForOperations(undoOperations, oldUndoCounts));
        }

        /// <summary>
        /// Determines whether splice with the given identfier was undone or not.
        /// </summary>
        /// <param name="spliceId">Identifier of the splice</param>
        /// <returns>true if splice was undone, otherwise false.</returns>
        private bool IsSpliceUndone(SpliceId spliceId)
        {
            return
                undoCountsBySpliceId.TryGetValue(spliceId, out var undoCount)
                && (undoCount & 1) == 1;
        }

        /// <summary>
        /// Checks if given <paramref name="operation"/> can be integrated into the document
        /// in the current state.
        /// Since editing is distributed across multiple sites, some operations may depend
        /// on others, that are not part of the document yet, so they will have to be deferred.
        /// </summary>
        /// <param name="operation">Operation to check</param>
        /// <returns>true if operation can be integrated, otherwise false.</returns>
        private bool CanIntegrateOperation(IOperation operation)
        {
            switch (operation)
            {
                case SpliceOperation spliceOperation:
                    if (!maxSeqsBySite.TryGetValue(spliceOperation.SpliceId.SiteId, out var maxSeqForSite))
                    {
                        maxSeqForSite = 0;
                    }

                    if (maxSeqForSite != spliceOperation.SpliceId.SequenceNumber - 1)
                    {
                        return false;
                    }

                    if (spliceOperation.Deletion != null)
                    {
                        if (!splitTreesBySpliceId.ContainsKey(spliceOperation.Deletion.LeftDependencyId)
                            || !splitTreesBySpliceId.ContainsKey(spliceOperation.Deletion.RightDependencyId))
                        {
                            // Does not have left and right dependency yet
                            return false;
                        }
                
                        foreach (var seqBySite in spliceOperation.Deletion.MaxSequenceNumberBySite)
                        {
                            if (!maxSeqsBySite.TryGetValue(seqBySite.Key, out var deletionSeqBySite))
                            {
                                deletionSeqBySite = 0;
                            }

                            if (seqBySite.Value > deletionSeqBySite)
                            {
                                return false;
                            }
                        }
                    }

                    if (spliceOperation.Insertion != null
                        && (!splitTreesBySpliceId.ContainsKey(spliceOperation.Insertion.LeftDependencyId)
                            || !splitTreesBySpliceId.ContainsKey(spliceOperation.Insertion.RightDependencyId)))
                    {
                        // Does not have left and right dependency yet
                        return false;
                    }

                    return true;

                case UndoOperation undoOperation:
                    return splitTreesBySpliceId.ContainsKey(undoOperation.SpliceId)
                        || deletionsBySpliceId.ContainsKey(undoOperation.SpliceId);

                case MarkersUpdateOperation markersUpdateOperation:
                    return true;

                default:
                    throw new InvalidOperationException(string.Format(Resources.UnknownOperationTypeErrorFormat, operation.GetType()));
            }
        }

        /// <summary>
        /// Integrates operations recieved from the other sites.
        /// </summary>
        /// <param name="operations">Operations to integrate</param>
        public DocumentStateUpdate IntegrateOperations(IList<IOperation> operations)
        {
            var integratedOperations = new List<IOperation>();
            Dictionary<SpliceId, int> oldUndoCounts = null;

            var operationIndex = 0;
            while (operationIndex < operations.Count)
            {
                var operation = operations[operationIndex++];

                if (!(operation is MarkersUpdateOperation))
                {
                    this.operations.Add(operation);
                }

                if (CanIntegrateOperation(operation))
                {
                    integratedOperations.Add(operation);
                    SpliceId? spliceId = null;

                    switch (operation)
                    {
                        case SpliceOperation spliceOperation:
                            spliceId = spliceOperation.SpliceId;

                            if (spliceOperation.Deletion != null)
                            {
                                IntegrateDeletion(spliceOperation.SpliceId, spliceOperation.Deletion);
                            }

                            if (spliceOperation.Insertion != null)
                            {
                                IntegrateInsertion(spliceOperation.SpliceId, spliceOperation.Insertion);
                            }

                            UpdateMaxSeqsBySite(spliceOperation.SpliceId);
                            break;

                        case UndoOperation undoOperation:
                            spliceId = undoOperation.SpliceId;

                            if (oldUndoCounts == null)
                            {
                                oldUndoCounts = new Dictionary<SpliceId, int>();
                            }

                            IntegrateUndo(undoOperation, oldUndoCounts);
                            break;
                    }

                    if (spliceId.HasValue)
                    {
                        CollectDeferredOperations(spliceId.Value, operations);
                    }
                }
                else
                {
                    DeferOperation(operation);
                }
            }

            return new DocumentStateUpdate(
                TextUpdatesForOperations(integratedOperations, oldUndoCounts),
                UpdateMarkersForOperations(integratedOperations));
        }

        /// <summary>
        /// Adds deferred operations that are dependent on the given <paramref name="spliceId"/> to the
        /// <paramref name="operations"/> collection.
        /// </summary>
        /// <param name="spliceId">Splice identifier of the dependency</param>
        /// <param name="operations">Operations to re-evaluate</param>
        private void CollectDeferredOperations(SpliceId spliceId, ICollection<IOperation> operations)
        {
            if (deferredOperationsByDependencyId.TryGetValue(spliceId, out var dependentOps))
            {
                foreach (var dependentOp in dependentOps)
                {
                    if (CanIntegrateOperation(dependentOp))
                    {
                        operations.Add(dependentOp);
                    }
                }

                deferredOperationsByDependencyId.Remove(spliceId);
            }
        }

        /// <summary>
        /// Queues an <paramref name="operation"/> to be applied later.
        /// </summary>
        /// <param name="operation">An operation to defer</param>
        private void DeferOperation(IOperation operation)
        {
            if (operation is SpliceOperation spliceOperation)
            {
                AddOperationDependency(
                    deferredOperationsByDependencyId,
                    new SpliceId(spliceOperation.SpliceId.SiteId, spliceOperation.SpliceId.SequenceNumber - 1),
                    operation);

                if (spliceOperation.Deletion != null)
                {
                    AddOperationDependency(
                        deferredOperationsByDependencyId,
                        spliceOperation.Deletion.LeftDependencyId,
                        operation);

                    AddOperationDependency(
                        deferredOperationsByDependencyId,
                        spliceOperation.Deletion.RightDependencyId,
                        operation);

                    foreach (var seqBySite in spliceOperation.Deletion.MaxSequenceNumberBySite)
                    {
                        AddOperationDependency(
                            deferredOperationsByDependencyId,
                            new SpliceId(seqBySite.Key, seqBySite.Value),
                            operation);
                    }
                }

                if (spliceOperation.Insertion != null)
                {
                    AddOperationDependency(
                        deferredOperationsByDependencyId,
                        spliceOperation.Insertion.LeftDependencyId,
                        operation);
                    AddOperationDependency(
                        deferredOperationsByDependencyId,
                        spliceOperation.Insertion.RightDependencyId,
                        operation);
                }
            }
            else if (operation is UndoOperation undoOperation)
            {
                AddOperationDependency(
                    deferredOperationsByDependencyId,
                    undoOperation.SpliceId,
                    operation);
            }
            else
            {
                throw new InvalidOperationException(string.Format(Resources.UnknownOperationTypeErrorFormat, operation.GetType()));
            }
        }

        /// <summary>
        /// Claims a <paramref name="dependency"/> to depend on <paramref name="operationSpliceId"/>.
        /// Dependency cannot be integrated into the document before its parent operation is integrated.
        /// </summary>
        /// <param name="map">Pending operations dependency map</param>
        /// <param name="operationSpliceId">Identifier of the parent operation</param>
        /// <param name="dependency">Operation dependency</param>
        private void AddOperationDependency<TDependency>(Dictionary<SpliceId, IList<TDependency>> map, SpliceId operationSpliceId, TDependency dependency)
        {
            if (!HasAppliedSplice(operationSpliceId))
            {
                if (!map.TryGetValue(operationSpliceId, out var dependencies))
                {
                    dependencies = new List<TDependency>();
                    map.Add(operationSpliceId, dependencies);
                }

                dependencies.Add(dependency);
            }
        }

        /// <summary>
        /// Checks whether operation with the given <paramref name="spliceId"/> was already applied.
        /// </summary>
        /// <param name="spliceId">Identifier of the splice</param>
        /// <returns>true if operation was already applied, otherwise false.</returns>
        private bool HasAppliedSplice(SpliceId spliceId)
        {
            return splitTreesBySpliceId.ContainsKey(spliceId)
                || deletionsBySpliceId.ContainsKey(spliceId);
        }

        /// <summary>
        /// Integrates text insertion to the document.
        /// </summary>
        /// <param name="spliceId">Identifier of the splice for the modification operation</param>
        /// <param name="insertion">Text insertion modification</param>
        private void IntegrateInsertion(SpliceId spliceId, TextInsertionModification insertion)
        {
            var originalRightDependency =
                FindSegmentStart(insertion.RightDependencyId, insertion.OffsetInRightDependency);
            var originalLeftDependency =
                FindSegmentEnd(insertion.LeftDependencyId, insertion.OffsetInLeftDependency);

            documentTree.SplayNode(originalLeftDependency);
            documentTree.SplayNode(originalRightDependency);

            var currentSegment = documentTree.GetSuccessor(originalLeftDependency);
            var leftDependency = originalLeftDependency;
            var rightDependency = originalRightDependency;
            while (currentSegment != rightDependency)
            {
                var leftDependencyIndex = documentTree.GetSegmentIndex(leftDependency);
                var rightDependencyIndex = documentTree.GetSegmentIndex(rightDependency);

                var currentSegmentLeftDependencyIndex =
                    documentTree.GetSegmentIndex(currentSegment.LeftDependency);
                var currentSegmentRightDependencyIndex =
                    documentTree.GetSegmentIndex(currentSegment.RightDependency);

                if (currentSegmentLeftDependencyIndex <= leftDependencyIndex
                    && currentSegmentRightDependencyIndex >= rightDependencyIndex)
                {
                    if (spliceId.SiteId < currentSegment.SpliceId.SiteId)
                    {
                        rightDependency = currentSegment;
                    }
                    else
                    {
                        leftDependency = currentSegment;
                    }

                    currentSegment = documentTree.GetSuccessor(leftDependency);
                }
                else
                {
                    currentSegment = documentTree.GetSuccessor(currentSegment);
                }
            }

            var newSegment =
                new Segment(
                    spliceId,
                    Point.Zero,
                    insertion.Text,
                    Point.GetExtentForText(insertion.Text))
                {
                    LeftDependency = originalLeftDependency,
                    RightDependency = originalRightDependency,
                };

            documentTree.InsertBetween(leftDependency, rightDependency, newSegment);
            splitTreesBySpliceId[spliceId] = new SplitTree(newSegment);
        }

        /// <summary>
        /// Integrates text deletion to the document.
        /// </summary>
        /// <param name="spliceId">Identifier of the splice for the modification operation</param>
        /// <param name="deletion">Text deletion modification</param>
        private void IntegrateDeletion(SpliceId spliceId, TextDeletionModification deletion)
        {
            deletionsBySpliceId[spliceId] = deletion;

            var left = FindSegmentStart(deletion.LeftDependencyId, deletion.OffsetInLeftDependency);
            var right = FindSegmentEnd(deletion.RightDependencyId, deletion.OffsetInRightDependency);

            var segment = left;
            while (true)
            {
                if (deletion.MaxSequenceNumberBySite == null
                    || !deletion.MaxSequenceNumberBySite.TryGetValue(segment.SpliceId.SiteId, out var maxSeq))
                {
                    maxSeq = 0;
                }
                
                if (segment.SpliceId.SequenceNumber <= maxSeq)
                {
                    documentTree.SplayNode(segment);
                    segment.Deletions.Add(spliceId);
                    documentTree.UpdateSubtreeExtent(segment);
                }

                if (segment == right)
                {
                    break;
                }

                segment = documentTree.GetSuccessor(segment);
            }
        }

        /// <summary>
        /// Integrates an undo operation into the document.
        /// </summary>
        /// <param name="undoOperation">Undo operation</param>
        /// <param name="oldUndoCounts">Number of undo actions per operation</param>
        private void IntegrateUndo(UndoOperation undoOperation, Dictionary<SpliceId, int> oldUndoCounts)
        {
            UpdateUndoCount(undoOperation.SpliceId, undoOperation.UndoCount, oldUndoCounts);
        }

        /// <summary>
        /// Updates number of undos for an operation with the given splice identifier.
        /// </summary>
        /// <param name="spliceId">Identifier of the splice for the operation to undo</param>
        /// <param name="newUndoCount">New number of undos for an operation</param>
        /// <param name="oldUndoCounts">Old number of undos per operation</param>
        private void UpdateUndoCount(SpliceId spliceId, int newUndoCount, Dictionary<SpliceId, int> oldUndoCounts)
        {
            if (!undoCountsBySpliceId.TryGetValue(spliceId, out var previousUndoCount))
            {
                previousUndoCount = 0;
            }

            if (newUndoCount <= previousUndoCount)
            {
                return;
            }

            oldUndoCounts[spliceId] = previousUndoCount;
            undoCountsBySpliceId[spliceId] = newUndoCount;

            var segmentsToUpdate = new List<Segment>();
            CollectSegments(spliceId, segmentsToUpdate, null, null);

            foreach (var segment in segmentsToUpdate)
            {
                var wasVisible = IsSegmentVisible(segment, oldUndoCounts, null);
                var isVisible = IsSegmentVisible(segment, null, null);
                if (isVisible != wasVisible)
                {
                    documentTree.SplayNode(segment);
                    documentTree.UpdateSubtreeExtent(segment);
                }
            }
        }

        /// <summary>
        /// Retrieves linear text changes for the given <paramref name="operations"/>.
        /// </summary>
        /// <param name="operations">Document operations</param>
        /// <param name="oldUndoCounts">Old number of undos per operation</param>
        /// <returns>Collection of linear text changes for given <paramref name="operations"/>.</returns>
        private List<TextUpdate> TextUpdatesForOperations(IEnumerable<IOperation> operations, Dictionary<SpliceId, int> oldUndoCounts)
        {
            var newSpliceIds = new HashSet<SpliceId>();
            var segmentStartPositions = new Dictionary<Segment, Point>();
            var segmentIndices = new Dictionary<Segment, int>();

            foreach (var operation in operations) 
            {
                if (operation is IModificationOperation modificationOperation)
                {
                    if (modificationOperation is SpliceOperation spliceOperation)
                    {
                        newSpliceIds.Add(spliceOperation.SpliceId);
                    }

                    CollectSegments(modificationOperation.SpliceId, null, segmentIndices, segmentStartPositions);
                }
            }

            return ComputeChangesForSegments(segmentIndices, segmentStartPositions, oldUndoCounts, newSpliceIds);
        }

        /// <summary>
        /// Collects text segments associated with the given splice identifier.
        /// </summary>
        /// <param name="spliceId">Identifier of the splice</param>
        /// <param name="segments">Collected segments</param>
        /// <param name="segmentIndices">Indices of the segments</param>
        /// <param name="segmentStartPositions">Start positions for the segments</param>
        /// <remarks>
        /// Either <paramref name="segments"/> or both <paramref name="segmentIndices"/> and <paramref name="segmentStartPositions"/>
        /// should be provided. The logic will branch based on what arguments are not null.
        /// </remarks>
        private void CollectSegments(
            SpliceId spliceId,
            IList<Segment> segments,
            Dictionary<Segment, int> segmentIndices,
            Dictionary<Segment, Point> segmentStartPositions)
        {
            if (splitTreesBySpliceId.TryGetValue(spliceId, out var insertionSplitTree))
            {
                var segment = insertionSplitTree.First;
                while (segment != null)
                {
                    if (segments != null)
                    {
                        segments.Add(segment);
                    }
                    else
                    {
                        segmentStartPositions[segment] = documentTree.GetSegmentPosition(segment);
                        segmentIndices[segment] = documentTree.GetSegmentIndex(segment);
                    }

                    segment = insertionSplitTree.GetSuccessor(segment);
                }
            }

            if (deletionsBySpliceId.TryGetValue(spliceId, out var deletion))
            {
                var left = FindSegmentStart(deletion.LeftDependencyId, deletion.OffsetInLeftDependency);
                var right = FindSegmentEnd(deletion.RightDependencyId, deletion.OffsetInRightDependency);
                var segment = left;
                while (true)
                {
                    if (!deletion.MaxSequenceNumberBySite.TryGetValue(segment.SpliceId.SiteId, out var maxSeq))
                    {
                        maxSeq = 0;
                    }

                    if (segment.SpliceId.SequenceNumber <= maxSeq)
                    {
                        if (segments != null)
                        {
                            segments.Add(segment);
                        }
                        else
                        {
                            segmentStartPositions[segment] = documentTree.GetSegmentPosition(segment);
                            segmentIndices[segment] = documentTree.GetSegmentIndex(segment);
                        }
                    }

                    if (segment == right)
                    {
                        break;
                    }

                    segment = documentTree.GetSuccessor(segment);
                }
            }
        }

        private List<TextUpdate> ComputeChangesForSegments(
            Dictionary<Segment, int> segmentIndices,
            Dictionary<Segment, Point> segmentStartPositions,
            Dictionary<SpliceId, int> oldUndoCounts,
            HashSet<SpliceId> newOperations)
        {
            // TODO: Better way to order keys by values ascending?
            var orderedSegments = segmentIndices.OrderBy(s => s.Value).Select(s => s.Key).ToArray();

            var changes = new List<TextUpdate>();
            TextUpdate lastChange = null;
            for (var segmentIndex = 0; segmentIndex < orderedSegments.Length; ++segmentIndex)
            {
                var segment = orderedSegments[segmentIndex];
                var visibleBefore = IsSegmentVisible(segment, oldUndoCounts, newOperations);
                var visibleAfter = IsSegmentVisible(segment, null, null);

                if (visibleBefore != visibleAfter)
                {
                    var segmentNewStart = segmentStartPositions[segment];
                    var segmentOldStart =
                        lastChange != null
                            ? lastChange.OldEnd.Traverse(Point.Traversal(segmentNewStart, lastChange.NewEnd))
                            : segmentNewStart;

                    if (visibleBefore)
                    {
                        if (changes.Count > 0 && lastChange.NewEnd.CompareTo(segmentNewStart) == 0)
                        {
                            lastChange.OldEnd = lastChange.OldEnd.Traverse(segment.Extent);
                            lastChange.OldText += segment.Text;
                        }
                        else
                        {
                            lastChange = new TextUpdate(
                                segmentOldStart,
                                segmentOldStart.Traverse(segment.Extent),
                                segment.Text,
                                segmentNewStart,
                                segmentNewStart,
                                string.Empty);

                            changes.Add(lastChange);
                        }
                    }
                    else
                    {
                        if (lastChange != null && lastChange.NewEnd.CompareTo(segmentNewStart) == 0)
                        {
                            lastChange.NewEnd = lastChange.NewEnd.Traverse(segment.Extent);
                            lastChange.NewText += segment.Text;
                        }
                        else
                        {
                            lastChange = new TextUpdate(
                                segmentOldStart,
                                segmentOldStart,
                                string.Empty,
                                segmentNewStart,
                                segmentNewStart.Traverse(segment.Extent),
                                segment.Text);

                            changes.Add(lastChange);
                        }
                    }
                }
            }

            return changes;
        }

        /// <summary>
        /// Creates a copy of the current document for a new site.
        /// </summary>
        /// <param name="siteId">New site identifier</param>
        /// <returns>Copy of the document with new site identifier.</returns>
        public Document Replicate(int siteId)
        {
            var replica = new Document(siteId);
            replica.IntegrateOperations(GetOperations().ToList());
            return replica;
        }

        /// <summary>
        /// Retrieves all operations that have been integrated in to the document.
        /// </summary>
        /// <returns>Collection of text operations and marker updates.</returns>
        public IReadOnlyList<IOperation> GetOperations()
        {
            var result = new List<IOperation>(operations.Count + markerLayersBySiteId.Count);

            foreach (var operation in operations)
            {
                result.Add(operation);
            }

            foreach (var layersMap in markerLayersBySiteId)
            {
                var siteMarkerLayers = new Dictionary<int, IReadOnlyDictionary<int, Marker<LogicalRange>>>(layersMap.Value.Count);

                foreach (var markersMap in layersMap.Value)
                {
                    siteMarkerLayers.Add(markersMap.Key, markersMap.Value);
                }

                result.Add(new MarkersUpdateOperation(layersMap.Key, siteMarkerLayers));
            }

            return result;
        }

        /// <summary>
        /// Populates document and undo stack from the given history.
        /// </summary>
        /// <param name="history">Document modification history</param>
        private void PopulateHistory(History history)
        {
            SetTextInRange(Point.Zero, Point.Zero, history.BaseText);
            nextCheckpointId = history.NextCheckpointId;

            var newUndoStack = new Stack<IUndoRecord>();
            foreach (var record in history.UndoStack.Concat(history.RedoStack.Reverse()))
            {
                if (record is TransactionHistoryRecord transactionRecord)
                {
                    var operations = new List<IOperation>(transactionRecord.Changes.Count);
                    var markersSnapshotBefore = SnapshotFromMarkers(transactionRecord.MarkersBefore);
                    for (var changeIndex = transactionRecord.Changes.Count - 1; changeIndex >= 0; --changeIndex)
                    {
                        var change = transactionRecord.Changes[changeIndex];
                        operations.Add(
                            SetTextInRange(change.OldStart, change.OldEnd, change.NewText));
                    }

                    var markersSnapshotAfter = SnapshotFromMarkers(transactionRecord.MarkersAfter);
                    newUndoStack.Push(
                        new TransactionRecord(0, operations, markersSnapshotBefore, markersSnapshotAfter));
                }
                else if (record is CheckpointHistoryRecord checkpointRecord)
                {
                    newUndoStack.Push(
                        new CheckpointRecord(checkpointRecord.Id, false, SnapshotFromMarkers(checkpointRecord.Markers)));
                }
                else
                {
                    throw new InvalidOperationException(string.Format(Resources.UnknownHistoryUndoRecordTypeErrorFormat, record.GetType()));
                }
            }

            undoStack = newUndoStack;
            foreach (var record in history.RedoStack)
            {
                if (record is TransactionHistoryRecord)
                {
                    Undo();
                }
            }
        }

        /// <summary>
        /// Prepares mapping of markers state based on the given set of <paramref name="operations"/>.
        /// </summary>
        /// <param name="operations">Operations to update markers from</param>
        /// <returns>Markers state per site, per layer.</returns>
        private Dictionary<int, Dictionary<int, Dictionary<int, Marker<Range>>>> UpdateMarkersForOperations(IEnumerable<IOperation> operations)
        {
            var markerUpdates = new Dictionary<int, Dictionary<int, Dictionary<int, Marker<Range>>>>();

            foreach (var operation in operations)
            {
                if (operation is MarkersUpdateOperation markersUpdateOperation)
                {
                    IntegrateMarkerUpdates(markerUpdates, markersUpdateOperation);
                }
                else if (operation is SpliceOperation spliceOperation)
                {
                    IntegrateDeferredMarkerUpdates(markerUpdates, spliceOperation.SpliceId);
                }
            }

            return markerUpdates;
        }

        /// <summary>
        /// Integrates marker updates into the given markers hierarchy from the given <paramref name="markersUpdateOperation"/>.
        /// </summary>
        /// <param name="markerUpdates">Target markers hierarchy (SiteId -> LayerId -> MarkerId -> Marker)</param>
        /// <param name="markersUpdateOperation">Markers update operation</param>
        private void IntegrateMarkerUpdates(
            Dictionary<int, Dictionary<int, Dictionary<int, Marker<Range>>>> markerUpdates,
            MarkersUpdateOperation markersUpdateOperation)
        {
            var layers = GetMarkerLayersForSiteId(markersUpdateOperation.SiteId);
            if (!markerUpdates.ContainsKey(markersUpdateOperation.SiteId))
            {
                markerUpdates.Add(markersUpdateOperation.SiteId, new Dictionary<int, Dictionary<int, Marker<Range>>>());
            }

            foreach (var layerUpdates in markersUpdateOperation.Updates)
            {
                var layerId = layerUpdates.Key;
                var updatesByMarkerId = layerUpdates.Value;

                if (!layers.TryGetValue(layerId, out var layer))
                {
                    layer = null;
                }

                if (updatesByMarkerId != null)
                {
                    if (layer == null)
                    {
                        layer = new Dictionary<int, Marker<LogicalRange>>();
                        layers[layerId] = layer;
                    }

                    if (!markerUpdates[markersUpdateOperation.SiteId].ContainsKey(layerId))
                    {
                        markerUpdates[markersUpdateOperation.SiteId][layerId] = new Dictionary<int, Marker<Range>>();
                    }

                    foreach (var marker in updatesByMarkerId)
                    {
                        var markerId = marker.Key;
                        var markerUpdate = marker.Value;

                        if (markerUpdate != null)
                        {
                            if (markerUpdate.Range != null && !CanResolveLogicalRange(markerUpdate.Range))
                            {
                                DeferMarkerUpdate(markersUpdateOperation.SiteId, layerId, markerId, markerUpdate);
                            }
                            else
                            {
                                IntegrateMarkerUpdate(markerUpdates, markersUpdateOperation.SiteId, layerId, markerId, markerUpdate);
                            }
                        }
                        else
                        {
                            if (layer.ContainsKey(markerId))
                            {
                                layer.Remove(markerId);
                                markerUpdates[markersUpdateOperation.SiteId][layerId][markerId] = null;
                            }

                            if (deferredMarkerUpdates.TryGetValue(markersUpdateOperation.SiteId, out var deferredUpdatesByLayerId))
                            {
                                if (deferredUpdatesByLayerId.TryGetValue(layerId, out var deferredUpdatesByMarkerId))
                                {
                                    deferredUpdatesByMarkerId.Remove(markerId);
                                }
                            }
                        }
                    }
                }
                else
                {
                    if (layer != null)
                    {
                        markerUpdates[markersUpdateOperation.SiteId][layerId] = null;
                        layers.Remove(layerId);
                    }

                    if (deferredMarkerUpdates.TryGetValue(markersUpdateOperation.SiteId, out var deferredUpdatesByLayerId))
                    {
                        deferredUpdatesByLayerId.Remove(layerId);
                    }
                }
            }
        }

        /// <summary>
        /// Integrates deferred marker updates that depend on the operation with given <paramref name="spliceId"/> into the provided markers hierarchy.
        /// </summary>
        /// <param name="markerUpdates">Target markers hierarchy (SiteId -> LayerId -> MarkerId -> Marker)</param>
        /// <param name="spliceId">Identifier of the splice for the operation that became available</param>
        private void IntegrateDeferredMarkerUpdates(
            Dictionary<int, Dictionary<int, Dictionary<int, Marker<Range>>>> markerUpdates,
            SpliceId spliceId)
        {
            if (deferredMarkerUpdatesByDependencyId.TryGetValue(spliceId, out var dependentMarkerUpdates))
            {
                foreach (var operation in dependentMarkerUpdates)
                {
                    if (deferredMarkerUpdates.TryGetValue(operation.SiteId, out var deferredUpdatesByLayerId))
                    {
                        if (deferredUpdatesByLayerId.TryGetValue(operation.LayerId, out var deferredUpdatesByMarkerId))
                        {
                            if (deferredUpdatesByMarkerId.TryGetValue(operation.MarkerId, out var deferredUpdate)
                                && CanResolveLogicalRange(deferredUpdate.Range))
                            {
                                IntegrateMarkerUpdate(markerUpdates, operation.SiteId, operation.LayerId, operation.MarkerId, deferredUpdate);
                            }
                        }
                    }
                }
                    
                deferredMarkerUpdatesByDependencyId.Remove(spliceId);
            }
        }

        /// <summary>
        /// Integrates single marker update into the provided markers hierarchy.
        /// </summary>
        /// <param name="markerUpdates">Target markers hierarchy (SiteId -> LayerId -> MarkerId -> Marker)</param>
        /// <param name="siteId">Identifier of the site that owns the marker</param>
        /// <param name="layerId">Identifier of the markers layer</param>
        /// <param name="markerId">Identifier of the marker</param>
        /// <param name="update">Updated marker information</param>
        private void IntegrateMarkerUpdate(
            Dictionary<int, Dictionary<int, Dictionary<int, Marker<Range>>>> markerUpdates,
            int siteId,
            int layerId,
            int markerId,
            Marker<LogicalRange> update)
        {
            if (!markerLayersBySiteId[siteId].TryGetValue(layerId, out var layer))
            {
                layer = new Dictionary<int, Marker<LogicalRange>>();
                markerLayersBySiteId[siteId].Add(layerId, layer);
            }

            layer[markerId] = update;

            if (!markerUpdates.TryGetValue(siteId, out var markerUpdatesForSite))
            {
                markerUpdates[siteId] = markerUpdatesForSite = new Dictionary<int, Dictionary<int, Marker<Range>>>();
            }

            if (!markerUpdatesForSite.TryGetValue(layerId, out var markerUpdatesForLayer))
            {
                markerUpdatesForSite[layerId] = markerUpdatesForLayer = new Dictionary<int, Marker<Range>>();
            }

            markerUpdatesForLayer[markerId] =
                new Marker<Range>(
                    update.Exclusive,
                    update.Reversed,
                    update.Tailed,
                    ResolveLogicalRange(update.Range, update.Exclusive));

            if (deferredMarkerUpdates.TryGetValue(siteId, out var deferredUpdatesByLayerId))
            {
                if (deferredUpdatesByLayerId.TryGetValue(layerId, out var deferredUpdatesByMarkerId))
                {
                    if (deferredUpdatesByMarkerId.ContainsKey(markerId))
                    {
                        deferredUpdatesByMarkerId.Remove(markerId);
                        if (deferredUpdatesByMarkerId.Count == 0)
                        {
                            deferredUpdatesByLayerId.Remove(layerId);
                            if (deferredUpdatesByLayerId.Count == 0)
                            {
                                deferredMarkerUpdates.Remove(siteId);
                            }
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Retrieves marker layers for the given site from the current document state.
        /// If no layers exist - empty mapping will be created.
        /// </summary>
        /// <param name="siteId">Identifier of the site</param>
        /// <returns>Mapping between markers layer identifier, marker identifier and actual marker for the given site.</returns>
        private Dictionary<int, Dictionary<int, Marker<LogicalRange>>> GetMarkerLayersForSiteId(int siteId)
        {
            if (!markerLayersBySiteId.TryGetValue(siteId, out var layers))
            {
                layers = new Dictionary<int, Dictionary<int, Marker<LogicalRange>>>();
                markerLayersBySiteId.Add(siteId, layers);
            }

            return layers;
        }

        private void DeferMarkerUpdate(int siteId, int layerId, int markerId, Marker<LogicalRange> markerUpdate)
        {
            var deferredMarkerUpdate = new DeferredMarkerUpdateOperation(siteId, layerId, markerId);
            AddOperationDependency(deferredMarkerUpdatesByDependencyId, markerUpdate.Range.StartDependencyId, deferredMarkerUpdate);
            AddOperationDependency(deferredMarkerUpdatesByDependencyId, markerUpdate.Range.EndDependencyId, deferredMarkerUpdate);

            if (!deferredMarkerUpdates.TryGetValue(siteId, out var deferredUpdatesByLayerId))
            {
                deferredUpdatesByLayerId = new Dictionary<int, Dictionary<int, Marker<LogicalRange>>>();
                deferredMarkerUpdates.Add(siteId, deferredUpdatesByLayerId);
            }

            if (!deferredUpdatesByLayerId.TryGetValue(layerId, out var deferredUpdatesByMarkerId))
            {
                deferredUpdatesByMarkerId = new Dictionary<int, Marker<LogicalRange>>();
                deferredUpdatesByLayerId.Add(layerId, deferredUpdatesByMarkerId);
            }

            deferredUpdatesByMarkerId[markerId] = markerUpdate;
        }

        /// <summary>
        /// Creates a copy of the given marker layers replacing ranges for all markers with
        /// logical ranges.
        /// </summary>
        /// <param name="layersById">Marker layers to take a snapshot of</param>
        /// <returns>Snapshot of the given marker layers.</returns>
        private IReadOnlyDictionary<int, IReadOnlyDictionary<int, Marker<LogicalRange>>> SnapshotFromMarkers(
            IReadOnlyDictionary<int, IReadOnlyDictionary<int, Marker<Range>>> layersById)
        {
            if (layersById == null)
            {
                return null;
            }

            var snapshot = new Dictionary<int, IReadOnlyDictionary<int, Marker<LogicalRange>>>(layersById.Count);

            foreach (var layer in layersById)
            {
                var markersById = layer.Value;
                var layerSnapshot = new Dictionary<int, Marker<LogicalRange>>(markersById.Count);
                
                foreach (var marker in markersById)
                {
                    var markerValue = marker.Value;
                    layerSnapshot.Add(
                        marker.Key,
                        new Marker<LogicalRange>(
                            markerValue.Exclusive,
                            markerValue.Reversed,
                            markerValue.Tailed,
                            GetLogicalRange(markerValue.Range, markerValue.Exclusive)));
                }

                snapshot.Add(layer.Key, layerSnapshot);
            }

            return snapshot;
        }

        /// <summary>
        /// Creates a copy of the given marker layers snapshot resolving logical ranges for all markers.
        /// </summary>
        /// <param name="snapshot">Marker layers snapshot</param>
        /// <returns>Resolved marker layers.</returns>
        private IReadOnlyDictionary<int, IReadOnlyDictionary<int, Marker<Range>>> MarkersFromSnapshot(
            IReadOnlyDictionary<int, IReadOnlyDictionary<int, Marker<LogicalRange>>> snapshot)
        {
            if (snapshot == null)
            {
                return null;
            }

            var layersById = new Dictionary<int, IReadOnlyDictionary<int, Marker<Range>>>(snapshot.Count);

            foreach (var layer in snapshot)
            {
                var layerSnapshot = layer.Value;
                var markersById = new Dictionary<int, Marker<Range>>(layerSnapshot.Count);
                
                foreach (var marker in layerSnapshot)
                {
                    var markerSnapshot = marker.Value;
                    markersById.Add(
                        marker.Key,
                        new Marker<Range>(
                            markerSnapshot.Exclusive,
                            markerSnapshot.Reversed,
                            markerSnapshot.Tailed,
                            ResolveLogicalRange(markerSnapshot.Range, false)));
                }

                layersById.Add(layer.Key, markersById);
            }

            return layersById;
        }

        /// <summary>
        /// Checks whether given logical range can be resolved.
        /// </summary>
        /// <param name="range">Logical range to be resolved</param>
        /// <returns>true if logical range can be resolved with the current document state, otherwise false.</returns>
        private bool CanResolveLogicalRange(LogicalRange range) {
            return HasAppliedSplice(range.StartDependencyId)
                && HasAppliedSplice(range.EndDependencyId);
        }

        /// <summary>
        /// Converts linear range to a logical range (represented by splice and offset within that splice).
        /// </summary>
        /// <param name="range">Linear range within the document</param>
        /// <param name="isExclusive">Flag indicating whether range excludes start and end positions</param>
        /// <returns>Logical range for the given linear range.</returns>
        private LogicalRange GetLogicalRange(Range range, bool isExclusive)
        {
            ( var startDependency, var offsetInStartDependency ) = FindSegment(range.Start, isExclusive);
            ( var endDependency, var offsetInEndDependency ) = FindSegment(range.End, !isExclusive || range.Start.CompareTo(range.End) == 0);

            return new LogicalRange(
                startDependency.SpliceId,
                offsetInStartDependency,
                endDependency.SpliceId,
                offsetInEndDependency);
        }

        /// <summary>
        /// Converts logical range to a linear range within the document.
        /// </summary>
        /// <param name="logicalRange">Logical range to convert</param>
        /// <param name="isExclusive">Flag indicating whether range excludes start and end positions</param>
        /// <returns>Linear range in the text document.</returns>
        private Range ResolveLogicalRange(LogicalRange logicalRange, bool isExclusive)
        {
            return new Range(
                ResolveLogicalPosition(
                    logicalRange.StartDependencyId,
                    logicalRange.OffsetInStartDependency,
                    isExclusive),
                ResolveLogicalPosition(
                    logicalRange.EndDependencyId,
                    logicalRange.OffsetInEndDependency,
                    !isExclusive || IsEmptyLogicalRange(logicalRange)));
        }

        /// <summary>
        /// Resolves logical position in the document.
        /// </summary>
        /// <param name="spliceId">Identifier of the splice owning</param>
        /// <param name="offset">Position offset within the splice</param>
        /// <param name="preferStart">Whether to prefer start of the following segment, if the position falls between two segments</param>
        /// <returns>The position in the document</returns>
        private Point ResolveLogicalPosition(SpliceId spliceId, Point offset, bool preferStart)
        {
            var splitTree = splitTreesBySpliceId[spliceId];
            var segment = splitTree.FindSegmentContainingOffset(offset);
            var nextSegmentOffset = segment.Offset.Traverse(segment.Extent);

            if (preferStart && offset.CompareTo(nextSegmentOffset) == 0)
            {
                segment = splitTree.GetSuccessor(segment) ?? segment;
            }

            var segmentStart = documentTree.GetSegmentPosition(segment);

            return
                IsSegmentVisible(segment, null, null)
                    ? segmentStart.Traverse(Point.Traversal(offset, segment.Offset))
                    : segmentStart;
        }

        /// <summary>
        /// Checks if logical range is empty.
        /// </summary>
        /// <param name="range">Logical range to check</param>
        /// <returns>true if given range is empty, otherwise false.</returns>
        private bool IsEmptyLogicalRange(LogicalRange range)
        {
            return range.StartDependencyId == range.EndDependencyId
                && range.OffsetInStartDependency.CompareTo(range.OffsetInEndDependency) == 0;
        }

        /// <summary>
        /// Inverts old and new properties on the <see cref="TextUpdate"/>s.
        /// </summary>
        /// <param name="textUpdates">Text updates to invert</param>
        /// <returns>Inverted text updates.</returns>
        private List<TextUpdate> InvertTextUpdates(IReadOnlyCollection<TextUpdate> textUpdates)
        {
            var invertedTextUpdates = new List<TextUpdate>(textUpdates.Count);

            foreach (var textUpdate in textUpdates)
            {
                invertedTextUpdates.Add(
                    new TextUpdate(
                        textUpdate.NewStart,
                        textUpdate.NewEnd,
                        textUpdate.NewText,
                        textUpdate.OldStart,
                        textUpdate.OldEnd,
                        textUpdate.OldText));
            }

            return invertedTextUpdates;
        }

        /// <summary>
        /// Retrieves current timestamp.
        /// </summary>
        /// <returns>Current timestamp.</returns>
        protected virtual long GetNow()
        {
            return DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        }
    }
}
