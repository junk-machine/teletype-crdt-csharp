namespace Teletype.Contracts
{
    /// <summary>
    /// Interface for all operations that modify the text in the document.
    /// These operations contain splice identifier.
    /// </summary>
    public interface IModificationOperation : IOperation
    {
        /// <summary>
        /// Gets the splice identifier for this operation.
        /// </summary>
        SpliceId SpliceId { get; }
    }
}
