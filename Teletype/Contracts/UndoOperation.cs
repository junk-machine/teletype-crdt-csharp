namespace Teletype.Contracts
{
    /// <summary>
    /// Defines an undo operation.
    /// </summary>
    /// <remarks>
    /// Equivalent of <code>{ 'type': 'undo' }</code> in the original implementation.
    /// </remarks>
    public class UndoOperation : IModificationOperation
    {
        /// <summary>
        /// Gets the splice identifier of the last undone operation.
        /// </summary>
        public SpliceId SpliceId { get; }

        /// <summary>
        /// Gets the number of undo actions performed as part of this operation.
        /// </summary>
        public int UndoCount { get; }

        /// <summary>
        /// Initializes a new isntance of the <see cref="UndoOperation"/> class
        /// with the provided splice identifier and number of undo actions.
        /// </summary>
        /// <param name="spliceId">Splice identifier of the last undone operation</param>
        /// <param name="undoCount">Number of undo actions</param>
        public UndoOperation(SpliceId spliceId, int undoCount)
        {
            SpliceId = spliceId;
            UndoCount = undoCount;
        }
    }
}
