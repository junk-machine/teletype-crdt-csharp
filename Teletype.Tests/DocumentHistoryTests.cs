using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using Teletype.Contracts;

namespace Teletype.Tests
{
    /// <summary>
    /// Defines unit tests for history operations (undo, redo, checkpoint, etc.) of the <see cref="Document"/> class.
    /// </summary>
    [TestClass]
    public class DocumentHistoryTests : DocumentTestsBase
    {
        [TestMethod]
        [Description("Basic undo and redo")]
        public void BasicUndoAndRedo()
        {
            var replicaA = BuildDocument(1);
            var replicaB = ReplicateDocument(2, replicaA);

            IntegrateOperations(replicaB, PerformInsert(replicaA, P(0, 0), "a1 "));
            IntegrateOperations(replicaA, PerformInsert(replicaB, P(0, 3), "b1 "));
            IntegrateOperations(replicaB, PerformInsert(replicaA, P(0, 6), "a2 "));
            IntegrateOperations(replicaA, PerformInsert(replicaB, P(0, 9), "b2"));
            IntegrateOperations(replicaA, PerformSetTextInRange(replicaB, P(0, 3), P(0, 5), "b3"));
            Assert.AreEqual("a1 b3 a2 b2", replicaA.TestLocalDocument.Text);
            Assert.AreEqual("a1 b3 a2 b2", replicaB.TestLocalDocument.Text);

            {
                IntegrateOperations(replicaA, PerformUndo(replicaB).Operations);
                Assert.AreEqual("a1 b1 a2 b2", replicaA.TestLocalDocument.Text);
                Assert.AreEqual("a1 b1 a2 b2", replicaB.TestLocalDocument.Text);
            }

            {
                IntegrateOperations(replicaB, PerformUndo(replicaA).Operations);
                Assert.AreEqual("a1 b1 b2", replicaA.TestLocalDocument.Text);
                Assert.AreEqual("a1 b1 b2", replicaB.TestLocalDocument.Text);
            }

            {
                IntegrateOperations(replicaB, PerformRedo(replicaA).Operations);
                Assert.AreEqual("a1 b1 a2 b2", replicaA.TestLocalDocument.Text);
                Assert.AreEqual("a1 b1 a2 b2", replicaB.TestLocalDocument.Text);
            }

            {
                IntegrateOperations(replicaA, PerformRedo(replicaB).Operations);
                Assert.AreEqual("a1 b3 a2 b2", replicaA.TestLocalDocument.Text);
                Assert.AreEqual("a1 b3 a2 b2", replicaB.TestLocalDocument.Text);
            }

            {
                IntegrateOperations(replicaA, PerformUndo(replicaB).Operations);
                Assert.AreEqual("a1 b1 a2 b2", replicaA.TestLocalDocument.Text);
                Assert.AreEqual("a1 b1 a2 b2", replicaB.TestLocalDocument.Text);
            }
        }

        [TestMethod]
        [Description("Does not allow the initial text to be undone")]
        public void DoNotUndoInitialText()
        {
            var document = BuildDocument(1, "hello");
            PerformInsert(document, P(0, 5), " world");
            Assert.IsNotNull(document.Undo());
            Assert.AreEqual("hello", document.GetText());
            Assert.IsNull(document.Undo());
            Assert.AreEqual("hello", document.GetText());
        }

        [TestMethod]
        [Description("Constructing the document with an initial history state")]
        public void ConstructDocumentFromHistory()
        {
            var document =
                new Document(
                    1,
                    new History(
                        baseText: "a ",
                        nextCheckpointId: 4,
                        undoStack: new IUndoHistoryRecord[]
                        {
                            new TransactionHistoryRecord(
                                new[] { new TextUpdate(P(0, 2), P(0, 2), "", P(0, 2), P(0, 4), "b ") },
                                MarkersForLayers()
                                    .Set(1, l => l
                                        .Set(1, new Marker<Range>(new Range(P(0, 0), P(0, 2))))).AsReadOnly(),
                                MarkersForLayers()
                                    .Set(1, l => l
                                        .Set(1, new Marker<Range>(new Range(P(0, 2), P(0, 4))))).AsReadOnly()),
                            new CheckpointHistoryRecord(
                                2,
                                MarkersForLayers()
                                    .Set(1, l => l
                                        .Set(1, new Marker<Range>(new Range(P(0, 2), P(0, 4))))).AsReadOnly()),
                            new TransactionHistoryRecord(
                                    new[] { new TextUpdate(P(0, 4), P(0, 4), "", P(0, 4), P(0, 6), "c ") },
                                    MarkersForLayers()
                                        .Set(1, l => l
                                            .Set(1, new Marker<Range>(new Range(P(0, 2), P(0, 4))))).AsReadOnly(),
                                    MarkersForLayers()
                                        .Set(1, l => l
                                            .Set(1, new Marker<Range>(new Range(P(0, 4), P(0, 6))))).AsReadOnly())
                        },
                        redoStack: new IUndoHistoryRecord[]
                        {
                            new TransactionHistoryRecord(
                                new[]
                                {
                                    new TextUpdate(P(0, 0), P(0, 0), "", P(0, 0), P(0, 2), "z "),
                                    new TextUpdate(P(0, 8), P(0, 8), "", P(0, 10), P(0, 11), "e")
                                },
                                MarkersForLayers()
                                    .Set(1, l => l
                                        .Set(1, new Marker<Range>(new Range(P(0, 6), P(0, 8))))).AsReadOnly(),
                                MarkersForLayers()
                                    .Set(1, l => l
                                        .Set(1, new Marker<Range>(new Range(P(0, 0), P(0, 2))))).AsReadOnly()),
                            new TransactionHistoryRecord(
                                new[] { new TextUpdate(P(0, 6), P(0, 6), "", P(0, 6), P(0, 8), "d ") },
                                MarkersForLayers()
                                    .Set(1, l => l
                                        .Set(1, new Marker<Range>(new Range(P(0, 4), P(0, 6))))).AsReadOnly(),
                                MarkersForLayers()
                                    .Set(1, l => l
                                        .Set(1, new Marker<Range>(new Range(P(0, 6), P(0, 8))))).AsReadOnly()),
                            new CheckpointHistoryRecord(
                                3,
                                MarkersForLayers()
                                    .Set(1, l => l
                                        .Set(1, new Marker<Range>(new Range(P(0, 4), P(0, 6))))).AsReadOnly())
                        }));

            Assert.AreEqual("a b c ", document.GetText());

            {
                var redoResults = document.Redo();
                Assert.AreEqual("a b c d ", document.GetText());
                AssertMarkersForLayers(
                    redoResults.Markers,
                    MarkersForLayers()
                        .Set(1, l => l.Set(1, new Marker<Range>(new Range(P(0, 6), P(0, 8))))).AsReadOnly());
            }

            {
                var redoResults = document.Redo();
                Assert.AreEqual("z a b c d e", document.GetText());
                AssertMarkersForLayers(
                    redoResults.Markers,
                    MarkersForLayers()
                        .Set(1, l => l.Set(1, new Marker<Range>(new Range(P(0, 0), P(0, 2))))).AsReadOnly());
            }

            {
                var undoResults = document.Undo();
                Assert.AreEqual("a b c d ", document.GetText());
                AssertMarkersForLayers(
                    undoResults.Markers,
                    MarkersForLayers()
                        .Set(1, l => l.Set(1, new Marker<Range>(new Range(P(0, 6), P(0, 8))))).AsReadOnly());
            }

            {
                var undoResults = document.Undo();
                Assert.AreEqual("a b c ", document.GetText());
                AssertMarkersForLayers(
                    undoResults.Markers,
                    MarkersForLayers()
                        .Set(1, l => l.Set(1, new Marker<Range>(new Range(P(0, 4), P(0, 6))))).AsReadOnly());
            }

            {
                var undoResults = document.Undo();
                Assert.AreEqual("a b ", document.GetText());
                AssertMarkersForLayers(
                    undoResults.Markers,
                    MarkersForLayers()
                        .Set(1, l => l.Set(1, new Marker<Range>(new Range(P(0, 2), P(0, 4))))).AsReadOnly());
            }

            {
                var undoResults = document.Undo();
                Assert.AreEqual("a ", document.GetText());
                AssertMarkersForLayers(
                    undoResults.Markers,
                    MarkersForLayers()
                        .Set(1, l => l.Set(1, new Marker<Range>(new Range(P(0, 0), P(0, 2))))).AsReadOnly());
            }

            Assert.IsNull(document.Undo());
            Assert.AreEqual("a ", document.GetText());

            // Redo everything
            while (document.Redo() != null) ;

            // Ensure we set the next checkpoint id appropriately
            var checkpoint = document.CreateCheckpoint();
            Assert.AreEqual(checkpoint, 4);

            {
                var revertResult = document.RevertToCheckpoint(3);
                Assert.AreEqual("a b c ", document.GetText());
                AssertMarkersForLayers(
                    revertResult.Markers,
                    MarkersForLayers()
                        .Set(1, l => l.Set(1, new Marker<Range>(new Range(P(0, 4), P(0, 6))))).AsReadOnly());
            }

            {
                var reverResult = document.RevertToCheckpoint(2);
                Assert.AreEqual(document.GetText(), "a b ");
                AssertMarkersForLayers(
                    reverResult.Markers,
                    MarkersForLayers()
                        .Set(1, l => l.Set(1, new Marker<Range>(new Range(P(0, 2), P(0, 4))))).AsReadOnly());
            }
        }

        [TestMethod]
        [Description("Clearing undo and redo stacks")]
        public void ClearUndoRedoStacks()
        {
            var document = BuildDocument(1);
            PerformInsert(document, P(0, 0), "a");
            document.ClearUndoStack();
            PerformInsert(document, P(0, 1), "b");
            PerformInsert(document, P(0, 2), "c");
            document.Undo();
            document.Undo();
            Assert.AreEqual("a", document.GetText());
            document.Redo();
            Assert.AreEqual("ab", document.GetText());
            document.ClearRedoStack();
            document.Redo();
            Assert.AreEqual("ab", document.GetText());

            // Clears the redo stack on changes
            document.Undo();
            PerformInsert(document, P(0, 1), "d");
            Assert.AreEqual("ad", document.GetText());
            document.Redo();
            Assert.AreEqual("ad", document.GetText());
        }

        [TestMethod]
        [Description("Grouping changes since a checkpoint")]
        public void GroupChangesSinceCheckpoint()
        {
            var replicaA = BuildDocument(1);
            var replicaB = ReplicateDocument(2, replicaA);

            IntegrateOperations(replicaB, PerformInsert(replicaA, P(0, 0), "a1 "));
            var checkpoint = replicaA.CreateCheckpoint(
                markers: MarkersForLayers()
                    .Set(1, l => l
                        .Set(1, new Marker<Range>(true, false, true, BuildRange(0, 1))))
                    .Set(2, l => l
                        .Set(1, new Marker<Range>(BuildRange(1, 2)))).AsReadOnly());

            IntegrateOperations(replicaB, PerformSetTextInRange(replicaA, P(0, 1), P(0, 3), "2 a3 "));
            IntegrateOperations(replicaB, PerformDelete(replicaA, P(0, 5), P(0, 6)));
            IntegrateOperations(replicaA, PerformInsert(replicaB, P(0, 0), "b1 "));
            Assert.AreEqual("b1 a2 a3", replicaA.TestLocalDocument.Text);
            Assert.AreEqual(replicaB.TestLocalDocument.Text, replicaA.TestLocalDocument.Text);
            AssertMarkersForSites(
                replicaB.TestLocalDocument.Markers.AsReadOnly(),
                replicaA.TestLocalDocument.Markers.AsReadOnly());

            var changes =
                replicaA.GroupChangesSinceCheckpoint(
                    checkpoint,
                    markers: MarkersForLayers()
                        .Set(1, l => l
                            .Set(1, new Marker<Range>(BuildRange(3, 5)))).AsReadOnly());

            CollectionAssert.AreEqual(
                changes.ToList(),
                new[] { new TextUpdate(P(0, 4), P(0, 6), "1 ", P(0, 4), P(0, 8), "2 a3") },
                TextUpdateComparer.Instance);

            Assert.AreEqual("b1 a2 a3", replicaA.TestLocalDocument.Text);
            Assert.AreEqual("b1 a2 a3", replicaB.TestLocalDocument.Text);

            {
                var undoResults = PerformUndo(replicaA);
                IntegrateOperations(replicaB, undoResults.Operations);
                Assert.AreEqual("b1 a1 ", replicaA.TestLocalDocument.Text);
                Assert.AreEqual(replicaA.TestLocalDocument.Text, replicaB.TestLocalDocument.Text);
                AssertMarkersForLayers(
                    undoResults.Markers,
                    MarkersForLayers()
                        .Set(1, l => l
                            .Set(1, new Marker<Range>(true, false, true, BuildRange(3, 4))))
                        .Set(2, l => l
                            .Set(1, new Marker<Range>(BuildRange(4, 5)))).AsReadOnly());
            }

            {
                var redoResults = PerformRedo(replicaA);
                IntegrateOperations(replicaB, redoResults.Operations);
                Assert.AreEqual("b1 a2 a3", replicaA.TestLocalDocument.Text);
                Assert.AreEqual(replicaA.TestLocalDocument.Text, replicaB.TestLocalDocument.Text);
                AssertMarkersForLayers(
                    redoResults.Markers,
                    MarkersForLayers()
                        .Set(1, l => l
                            .Set(1, new Marker<Range>(BuildRange(3, 5)))).AsReadOnly());
            }

            IntegrateOperations(replicaA, PerformUndo(replicaB).Operations);

            {
                var undoResults = PerformUndo(replicaA);
                IntegrateOperations(replicaB, undoResults.Operations);
                Assert.AreEqual("a1 ", replicaA.TestLocalDocument.Text);
                Assert.AreEqual(replicaA.TestLocalDocument.Text, replicaB.TestLocalDocument.Text);
                AssertMarkersForLayers(
                    undoResults.Markers,
                    MarkersForLayers()
                        .Set(1, l => l
                            .Set(1, new Marker<Range>(true, false, true, BuildRange(0, 1))))
                        .Set(2, l => l
                            .Set(1, new Marker<Range>(BuildRange(1, 2)))).AsReadOnly());
            }

            // Delete checkpoint
            CollectionAssert.AreEquivalent(replicaA.GroupChangesSinceCheckpoint(checkpoint, true).ToArray(), Array.Empty<TextUpdate>());
            Assert.IsNull(replicaA.GroupChangesSinceCheckpoint(checkpoint));
        }

        [TestMethod]
        [Description("Does not allow grouping changes past a barrier checkpoint")]
        public void DoesNotGroupChangesPastBarrier()
        {
            var document = BuildDocument(1);

            var checkpointBeforeBarrier = document.CreateCheckpoint(false);
            PerformInsert(document, P(0, 0), "a");
            var barrierCheckpoint = document.CreateCheckpoint(true);
            PerformInsert(document, P(0, 1), "b");
            Assert.IsNull(document.GroupChangesSinceCheckpoint(checkpointBeforeBarrier));

            PerformInsert(document, P(0, 2), "c");
            document.CreateCheckpoint(false);
            var changes = document.GroupChangesSinceCheckpoint(barrierCheckpoint);
            CollectionAssert.AreEqual(
                changes.ToList(),
                new[] { new TextUpdate(P(0, 1), P(0, 1), "", P(0, 1), P(0, 3), "bc") },
                TextUpdateComparer.Instance);
        }

        [TestMethod]
        [Description("Reverting to a checkpoint")]
        public void RevertToCheckpoint()
        {
            var replicaA = BuildDocument(1);
            var replicaB = ReplicateDocument(2, replicaA);

            IntegrateOperations(replicaB, PerformInsert(replicaA, P(0, 0), "a1 "));
            var checkpoint = replicaA.CreateCheckpoint(
                markers: MarkersForLayers()
                    .Set(1, l => l
                        .Set(1, new Marker<Range>(true, false, true, BuildRange(0, 1))))
                    .Set(2, l => l
                        .Set(1, new Marker<Range>(BuildRange(1, 2)))).AsReadOnly());

            IntegrateOperations(replicaB, PerformSetTextInRange(replicaA, P(0, 1), P(0, 3), "2 a3 "));
            IntegrateOperations(replicaB, PerformDelete(replicaA, P(0, 5), P(0, 6)));
            IntegrateOperations(replicaA, PerformInsert(replicaB, P(0, 0), "b1 "));

            Assert.AreEqual("b1 a2 a3", replicaA.TestLocalDocument.Text);
            Assert.AreEqual(replicaA.TestLocalDocument.Text, replicaB.TestLocalDocument.Text);

            var revertResults = PerformRevertToCheckpoint(replicaA, checkpoint);
            IntegrateOperations(replicaB, revertResults.Operations);
            Assert.AreEqual("b1 a1 ", replicaA.TestLocalDocument.Text);
            Assert.AreEqual(replicaA.TestLocalDocument.Text, replicaB.TestLocalDocument.Text);
            AssertMarkersForLayers(
                revertResults.Markers,
                MarkersForLayers()
                    .Set(1, l => l
                        .Set(1, new Marker<Range>(true, false, true, BuildRange(3, 4))))
                    .Set(2, l => l
                        .Set(1, new Marker<Range>(BuildRange(4, 5)))).AsReadOnly());

            // Delete checkpoint
            replicaA.RevertToCheckpoint(checkpoint, true);
            Assert.IsNull(replicaA.RevertToCheckpoint(checkpoint));
        }

        [TestMethod]
        [Description("Does not allow reverting past a barrier checkpoint")]
        public void DoesNotRevertPastBarrier()
        {
            var document = BuildDocument(1);
            var checkpointBeforeBarrier = document.CreateCheckpoint(false);
            PerformInsert(document, P(0, 0), "a");
            document.CreateCheckpoint(true);

            Assert.IsNull(document.RevertToCheckpoint(checkpointBeforeBarrier));
            Assert.AreEqual("a", document.GetText());

            PerformInsert(document, P(0, 1), "b");
            Assert.IsNull(document.RevertToCheckpoint(checkpointBeforeBarrier));
            Assert.AreEqual("ab", document.GetText());
        }

        [TestMethod]
        [Description("Getting changes since checkpoint")]
        public void GetChangesSinceCheckpoint()
        {
            var replicaA = BuildDocument(1);
            var replicaB = ReplicateDocument(2, replicaA);

            IntegrateOperations(replicaB, PerformInsert(replicaA, P(0, 0), "a1 "));
            var checkpoint = replicaA.CreateCheckpoint();
            IntegrateOperations(replicaB, PerformSetTextInRange(replicaA, P(0, 1), P(0, 3), "2 a3 "));
            IntegrateOperations(replicaB, PerformDelete(replicaA, P(0, 5), P(0, 6)));
            IntegrateOperations(replicaA, PerformInsert(replicaB, P(0, 0), "b1 "));
            Assert.AreEqual("b1 a2 a3", replicaA.TestLocalDocument.Text);

            var changesSinceCheckpoint = replicaA.GetChangesSinceCheckpoint(checkpoint);
            foreach (var change in changesSinceCheckpoint.Reverse())
            {
                replicaA.TestLocalDocument.SetTextInRange(change.NewStart, change.NewEnd, change.OldText);
            }

            Assert.AreEqual("b1 a1 ", replicaA.TestLocalDocument.Text);

            // Ensure we don't modify the undo stack when getting changes since checkpoint (regression).
            CollectionAssert.AreEqual(
                replicaA.GetChangesSinceCheckpoint(checkpoint).ToArray(),
                changesSinceCheckpoint.ToArray(),
                TextUpdateComparer.Instance);
        }

        [TestMethod]
        [Description("Undoing and redoing an operation that occurred adjacent to a checkpoint")]
        public void UndoRedoOperationNextToCheckpoint()
        {
            var document = BuildDocument(1);
            PerformInsert(document, P(0, 0), "a");
            PerformInsert(document, P(0, 1), "b");
            document.CreateCheckpoint();
            PerformInsert(document, P(0, 2), "c");

            document.Undo();
            Assert.AreEqual("ab", document.GetText());
            document.Undo();
            Assert.AreEqual("a", document.GetText());
            document.Redo();
            Assert.AreEqual("ab", document.GetText());
            document.Redo();
            Assert.AreEqual("abc", document.GetText());
        }

        [TestMethod]
        [Description("Reverting to a checkpoint after undoing and redoing an operation")]
        public void RevertToCheckpointAfterUndoRedo()
        {
            var document = BuildDocument(1);

            PerformInsert(document, P(0, 0), "a");
            var checkpoint1 = document.CreateCheckpoint();
            PerformInsert(document, P(0, 1), "b");
            var checkpoint2 = document.CreateCheckpoint();

            document.Undo();
            Assert.AreEqual("a", document.GetText());
            document.Redo();
            Assert.AreEqual("ab", document.GetText());

            PerformInsert(document, P(0, 2), "c");

            document.RevertToCheckpoint(checkpoint2);
            Assert.AreEqual("ab", document.GetText());

            document.RevertToCheckpoint(checkpoint1);
            Assert.AreEqual("a", document.GetText());
        }

        [TestMethod]
        [Description("Undoing preserves checkpoint created prior to any operations")]
        public void UndoPreservesOlderCheckpoints()
        {
            var document = BuildDocument(1);
            var checkpoint = document.CreateCheckpoint();
            document.Undo();
            PerformInsert(document, P(0, 0), "a");

            document.RevertToCheckpoint(checkpoint);
            Assert.AreEqual("", document.GetText());
        }

        [TestMethod]
        [Description("Does not allow undoing past a barrier checkpoint")]
        public void DoesNotUndoPastBarrier()
        {
            var document = BuildDocument(1);
            PerformInsert(document, P(0, 0), "a");
            PerformInsert(document, P(0, 1), "b");
            document.CreateCheckpoint(true);
            PerformInsert(document, P(0, 2), "c");
            document.CreateCheckpoint(false);

            Assert.AreEqual("abc", document.GetText());
            document.Undo();
            Assert.AreEqual("ab", document.GetText());
            Assert.IsNull(document.Undo());
            Assert.AreEqual("ab", document.GetText());
        }

        [TestMethod]
        [Description("Does not add empty transactions to the undo stack")]
        public void DoesNotAddEmptyUndoTransactions()
        {
            var replicaA = BuildDocument(1);
            var replicaB = ReplicateDocument(2, replicaA);
            IntegrateOperations(replicaB, PerformInsert(replicaA, P(0, 0), "a"));
            IntegrateOperations(replicaB, PerformInsert(replicaA, P(0, 1), "b"));
            var checkpoint = replicaA.CreateCheckpoint();
            IntegrateOperations(replicaA, PerformInsert(replicaB, P(0, 2), "c"));
            replicaA.GroupChangesSinceCheckpoint(checkpoint);
            IntegrateOperations(replicaB, PerformUndo(replicaA).Operations);

            Assert.AreEqual("ac", replicaA.TestLocalDocument.Text);
            Assert.AreEqual("ac", replicaB.TestLocalDocument.Text);
        }

        [TestMethod]
        [Description("Grouping the last 2 transactions")]
        public void GroupLastTwoTransactions()
        {
            var document = BuildDocument(1);
            PerformInsert(document, P(0, 0), "a");
            PerformInsert(document, P(0, 1), "b");
            var checkpoint1 = document.CreateCheckpoint();
            PerformInsert(document, P(0, 2), "c");
            var checkpoint2 = document.CreateCheckpoint();

            Assert.IsTrue(document.GroupLastChanges());
            Assert.AreEqual("abc", document.GetText());
            document.Undo();
            Assert.AreEqual("a", document.GetText());
            document.Redo();
            Assert.AreEqual("abc", document.GetText());
            Assert.IsNull(document.RevertToCheckpoint(checkpoint1));
            PerformInsert(document, P(0, 3), "d");
            Assert.IsNull(document.RevertToCheckpoint(checkpoint2));

            // Can't group past barrier checkpoints
            var checkpoint3 = document.CreateCheckpoint(true);
            PerformInsert(document, P(0, 4), "e");
            Assert.IsFalse(document.GroupLastChanges());
            Assert.IsNotNull(document.RevertToCheckpoint(checkpoint3));
            Assert.AreEqual("abcd", document.GetText());
        }

        [TestMethod]
        [Description("Applying a grouping interval")]
        public void AppliesGroupingInterval()
        {
            var document = BuildDocument(1);

            var initialMarkers =
                MarkersForLayers()
                    .Set(1, l => l
                        .Set(1, new Marker<Range>(BuildRange(0, 0)))).AsReadOnly();

            document.Now = 0;
            var checkpoint1 = document.CreateCheckpoint(markers: initialMarkers);
            PerformInsert(document, P(0, 0), "a");
            var markersAfterInsertion1 =
                MarkersForLayers()
                    .Set(1, l => l
                        .Set(1, new Marker<Range>(BuildRange(1, 1)))).AsReadOnly();

            document.GroupChangesSinceCheckpoint(checkpoint1, true, markersAfterInsertion1);
            document.ApplyGroupingInterval(101);

            document.Now += 100;
            var checkpoint2 = document.CreateCheckpoint(markers: markersAfterInsertion1);
            PerformInsert(document, P(0, 1), "b");
            var markersAfterInsertion2 =
                MarkersForLayers()
                    .Set(1, l => l
                        .Set(1, new Marker<Range>(BuildRange(2, 2)))).AsReadOnly();

            document.GroupChangesSinceCheckpoint(checkpoint2, true, markersAfterInsertion2);
            document.ApplyGroupingInterval(201);

            document.Now += 200;
            var checkpoint3 = document.CreateCheckpoint(markers: markersAfterInsertion2);
            PerformInsert(document, P(0, 2), "c");
            var markersAfterInsertion3 =
                MarkersForLayers()
                    .Set(1, l => l
                        .Set(1, new Marker<Range>(BuildRange(3, 3)))).AsReadOnly();

            document.GroupChangesSinceCheckpoint(checkpoint3, true, markersAfterInsertion3);
            document.ApplyGroupingInterval(201);

            // Not grouped with previous transaction because its associated grouping
            // interval is 201 and we always respect the minimum associated interval
            // between the last and penultimate transaction.
            document.Now += 300;
            var checkpoint4 = document.CreateCheckpoint(markers: markersAfterInsertion3);
            PerformInsert(document, P(0, 3), "d");
            var markersAfterInsertion4 =
                MarkersForLayers()
                    .Set(1, l => l
                        .Set(1, new Marker<Range>(BuildRange(4, 4)))).AsReadOnly();
            document.GroupChangesSinceCheckpoint(checkpoint4, true, markersAfterInsertion4);
            document.ApplyGroupingInterval(301);

            Assert.AreEqual("abcd", document.TestLocalDocument.Text);

            {
                var undoResult = PerformUndo(document);
                Assert.AreEqual("abc", document.TestLocalDocument.Text);
                AssertMarkersForLayers(undoResult.Markers, markersAfterInsertion3);
            }

            {
                var undoResult = PerformUndo(document);
                Assert.AreEqual("", document.TestLocalDocument.Text);
                AssertMarkersForLayers(undoResult.Markers, initialMarkers);
            }

            {
                var redoResult = PerformRedo(document);
                Assert.AreEqual("abc", document.TestLocalDocument.Text);
                AssertMarkersForLayers(redoResult.Markers, markersAfterInsertion3);
            }

            {
                var redoResult = PerformRedo(document);
                Assert.AreEqual("abcd", document.TestLocalDocument.Text);
                AssertMarkersForLayers(redoResult.Markers, markersAfterInsertion4);
            }
        }

        [TestMethod]
        [Description("Getting the state of the history")]
        public void GetHistory()
        {
            var document = BuildDocument(1, "a ");

            PerformInsert(document, P(0, 2), "b ");
            PerformInsert(document, P(0, 4), "c ");

            var checkpoint1 =
                document.CreateCheckpoint(
                    markers: MarkersForLayers()
                        .Set(1, l => l
                            .Set(1, new Marker<Range>(BuildRange(2, 5)))).AsReadOnly());

            PerformInsert(document, P(0, 0), "d ");
            PerformInsert(document, P(0, 8), "e ");

            document.GroupChangesSinceCheckpoint(
                checkpoint1,
                markers: MarkersForLayers()
                    .Set(1, l => l
                        .Set(1, new Marker<Range>(BuildRange(1, 3)))).AsReadOnly());

            PerformInsert(document, P(0, 10), "f ");

            var checkpoint2 =
                document.CreateCheckpoint(
                    markers: MarkersForLayers()
                        .Set(1, l => l
                            .Set(1, new Marker<Range>(BuildRange(0, 1)))).AsReadOnly());

            PerformInsert(document, P(0, 10), "g ");

            document.Undo();
            document.Undo();

            var replica = ReplicateDocument(2, document);

            var history = document.GetHistory(3);

            // The current implementation of GetHistory temporarily mutates the replica.
            // Here we make sure we restore the state of the document and its undo counts.
            Assert.AreEqual(replica.GetText(), document.GetText());
            IntegrateOperations(document, replica.UndoRedoOperations(replica.GetOperations().Where(op => !(op is MarkersUpdateOperation))).Item1);
            Assert.AreEqual(replica.GetText(), document.GetText());

            Assert.AreEqual(3, history.NextCheckpointId);
            CollectionAssert.AreEqual(
                history.UndoStack.ToArray(),
                new IUndoHistoryRecord[]
                {
                    new TransactionHistoryRecord(
                        new[] { new TextUpdate(P(0, 4), P(0, 4), "", P(0, 4), P(0, 6), "c ") },
                        null,
                        null),
                    new CheckpointHistoryRecord(
                        1,
                        MarkersForLayers()
                            .Set(1, l => l
                                .Set(1, new Marker<Range>(BuildRange(2, 5)))).AsReadOnly()),
                    new TransactionHistoryRecord(
                        new[]
                        {
                            new TextUpdate(P(0, 0), P(0, 0), "", P(0, 0), P(0, 2), "d "),
                            new TextUpdate(P(0, 6), P(0, 6), "", P(0, 8), P(0, 10), "e ")
                        },
                        MarkersForLayers()
                            .Set(1, l => l
                                .Set(1, new Marker<Range>(BuildRange(2, 5)))).AsReadOnly(),
                        MarkersForLayers()
                            .Set(1, l => l
                                .Set(1, new Marker<Range>(BuildRange(1, 3)))).AsReadOnly())
                },
                UndoHistoryRecordComparer.Instance);

            CollectionAssert.AreEqual(
                history.RedoStack.ToArray(),
                new IUndoHistoryRecord[]
                {
                    new TransactionHistoryRecord(
                        new[] { new TextUpdate(P(0, 10), P(0, 10), "", P(0, 10), P(0, 12), "g ") },
                        null,
                        null),
                    new CheckpointHistoryRecord(
                        2,
                        MarkersForLayers()
                            .Set(1, l => l
                                .Set(1, new Marker<Range>(BuildRange(0, 1)))).AsReadOnly()),
                    new TransactionHistoryRecord(
                        new[] { new TextUpdate(P(0, 10), P(0, 10), "", P(0, 10), P(0, 12), "f ") },
                        null,
                        null)
                },
                UndoHistoryRecordComparer.Instance);
        }
        
        #region Helper methods

        /// <summary>
        /// Reverts operations from the last transaction on the undo stack.
        /// </summary>
        /// <param name="replica">Teletype document instance</param>
        /// <returns>Results of undo action.</returns>
        private UndoRedoResults PerformUndo(TestDocument replica)
        {
            var undoResults = replica.Undo();
            replica.TestLocalDocument.UpdateText(undoResults.TextUpdates);
            return undoResults;
        }

        /// <summary>
        /// Applies operations from the latest transaction on the redo stack.
        /// </summary>
        /// <param name="replica">Teletype document instance</param>
        /// <returns>Results of redo action.</returns>
        private UndoRedoResults PerformRedo(TestDocument replica)
        {
            var redoResults = replica.Redo();
            replica.TestLocalDocument.UpdateText(redoResults.TextUpdates);
            return redoResults;
        }

        /// <summary>
        /// Reverts document to the checkpoint with given identifier.
        /// </summary>
        /// <param name="replica">Teletype document instance</param>
        /// <param name="checkpointId">Identifier of the checkpoint</param>
        private UndoRedoResults PerformRevertToCheckpoint(TestDocument replica, int checkpointId)
        {
            var revertResults = replica.RevertToCheckpoint(checkpointId);
            replica.TestLocalDocument.UpdateText(revertResults.TextUpdates);
            return revertResults;
        }

        /// <summary>
        /// Creates new range on line zero spanning characters from <paramref name="startColumn"/> to <paramref name="endColumn"/>.
        /// </summary>
        /// <param name="startColumn">Start column index</param>
        /// <param name="endColumn">End column index</param>
        /// <returns>Newly created range</returns>
        private Range BuildRange(int startColumn, int endColumn)
        {
            return new Range(P(0, startColumn), P(0, endColumn));
        }

        /// <summary>
        /// Equality comparer implementation for <see cref="TextUpdate"/> class.
        /// </summary>
        /// <remarks>
        /// This implementation relies on existing assertion helpers to compare text updates and markers,
        /// so we define it as a nested class here, rather than in a separate file.
        /// </remarks>
        private sealed class UndoHistoryRecordComparer : Comparer<IUndoHistoryRecord>
        {
            /// <summary>
            /// Singleton instance of the comparer.
            /// </summary>
            public static readonly UndoHistoryRecordComparer Instance = new UndoHistoryRecordComparer();

            /// <summary>
            /// Compares two instances of <see cref="IUndoHistoryRecord"/> class.
            /// </summary>
            /// <param name="x">First instance</param>
            /// <param name="y">Second instance</param>
            /// <returns>Zero if instances are the same, otherwise non-zero value.</returns>
            public override int Compare(IUndoHistoryRecord x, IUndoHistoryRecord y)
            {
                if (x is TransactionHistoryRecord transactionX && y is TransactionHistoryRecord transactionY)
                {
                    try
                    {
                        CollectionAssert.AreEqual(
                            transactionX.Changes.ToArray(),
                            transactionY.Changes.ToArray(),
                            TextUpdateComparer.Instance);

                        AssertMarkersForLayers(
                            transactionX.MarkersBefore,
                            transactionY.MarkersBefore);

                        AssertMarkersForLayers(
                            transactionX.MarkersAfter,
                            transactionY.MarkersAfter);
                    }
                    catch (AssertFailedException)
                    {
                        return 1;
                    }
                }
                else if (x is CheckpointHistoryRecord checkpointX && y is CheckpointHistoryRecord checkpointY)
                {
                    try
                    {
                        Assert.AreEqual(checkpointX.Id, checkpointY.Id);
                        AssertMarkersForLayers(checkpointX.Markers, checkpointY.Markers);
                    }
                    catch (AssertFailedException)
                    {
                        return 1;
                    }
                }
                else
                {
                    return -1;
                }

                return 0;
            }
        }

        #endregion Helper methods
    }
}
