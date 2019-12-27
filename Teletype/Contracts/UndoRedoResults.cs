using System.Collections.Generic;

namespace Teletype.Contracts
{
    /// <summary>
    /// Contains results of undo/redo action.
    /// </summary>
    public sealed class UndoRedoResults
    {
        /// <summary>
        /// Gets the list of undo operations.
        /// </summary>
        public IReadOnlyList<UndoOperation> Operations { get; }

        /// <summary>
        /// Gets linear text updates that represent the undo action.
        /// </summary>
        public IReadOnlyList<TextUpdate> TextUpdates { get; }

        /// <summary>
        /// Gets the markers state after the undo action.
        /// </summary>
        public IReadOnlyDictionary<int, IReadOnlyDictionary<int, Marker<Range>>> Markers { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="UndoRedoResults"/> class
        /// with the provided list of undo operations, text updates and markers updates.
        /// </summary>
        /// <param name="operations">Undo operations</param>
        /// <param name="textUpdates">Linear text updates</param>
        /// <param name="markers">Updates for document markers</param>
        public UndoRedoResults(
            IReadOnlyList<UndoOperation> operations,
            IReadOnlyList<TextUpdate> textUpdates,
            IReadOnlyDictionary<int, IReadOnlyDictionary<int, Marker<Range>>> markers)
        {
            Operations = operations;
            TextUpdates = textUpdates;
            Markers = markers;
        }
    }
}
