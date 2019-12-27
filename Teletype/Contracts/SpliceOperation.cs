namespace Teletype.Contracts
{
    /// <summary>
    /// Defines text insertion or deletion for a given site.
    /// </summary>
    /// <remarks>
    /// Equivalent of <code>{ 'type': 'splice' }</code> in the original implementation.
    /// </remarks>
    public class SpliceOperation : IModificationOperation
    {
        /// <summary>
        /// Gets the splice identifier for this operation.
        /// </summary>
        public SpliceId SpliceId { get; }

        /// <summary>
        /// Gets the text deletion modification associated with this operation.
        /// </summary>
        public TextDeletionModification Deletion { get; }

        /// <summary>
        /// Gets the text insertion modification associated with this operation.
        /// </summary>
        public TextInsertionModification Insertion { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="SpliceOperation"/> class
        /// with the provided splice identifiert, deletion and insertion modifications.
        /// </summary>
        /// <param name="spliceId">Splice identifier for this operation</param>
        /// <param name="deletion">Text deletion modification</param>
        /// <param name="insertion">Text insertion modification</param>
        public SpliceOperation(SpliceId spliceId, TextDeletionModification deletion, TextInsertionModification insertion)
        {
            SpliceId = spliceId;
            Deletion = deletion;
            Insertion = insertion;
        }
    }
}
