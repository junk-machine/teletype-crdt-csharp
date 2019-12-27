using System.Collections.Generic;

namespace Teletype.Contracts
{
    /// <summary>
    /// Defines document modification history.
    /// </summary>
    public class History
    {
        /// <summary>
        /// Gets the base text of the document before the history was recorded.
        /// </summary>
        public string BaseText { get; }

        /// <summary>
        /// Gets the next checkpoint identifier.
        /// </summary>
        public int NextCheckpointId { get; }

        /// <summary>
        /// Gets the records on the undo stack.
        /// </summary>
        public IEnumerable<IUndoHistoryRecord> UndoStack { get; }

        /// <summary>
        /// Gets the records on the redo stack.
        /// </summary>
        public IEnumerable<IUndoHistoryRecord> RedoStack { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="History"/> class
        /// with the provided base text, next checkpoint identifier, undo and redo stacks.
        /// </summary>
        /// <param name="baseText">Document text before the history was recorded</param>
        /// <param name="nextCheckpointId">Next checkpoint identifier</param>
        /// <param name="undoStack">Records on the undo stack</param>
        /// <param name="redoStack">Records on the redo stack</param>
        public History(
            string baseText,
            int nextCheckpointId,
            IEnumerable<IUndoHistoryRecord> undoStack,
            IEnumerable<IUndoHistoryRecord> redoStack)
        {
            BaseText = baseText;
            NextCheckpointId = nextCheckpointId;
            UndoStack = undoStack;
            RedoStack = redoStack;
        }
    }
}
