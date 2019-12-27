namespace Teletype.Contracts
{
    /// <summary>
    /// Base class for any text modification: insertion, deletion, etc.
    /// </summary>
    public abstract class TextModificationBase
    {
        /// <summary>
        /// Gets the identifier of the splice where text operation starts.
        /// </summary>
        public SpliceId LeftDependencyId { get; }

        /// <summary>
        /// Gets the character offset in starting splice.
        /// </summary>
        public Point OffsetInLeftDependency { get; }

        /// <summary>
        /// Identifier of the splice where text operation ends.
        /// </summary>
        public SpliceId RightDependencyId { get; }

        /// <summary>
        /// Gets the character offset in ending splice.
        /// </summary>
        public Point OffsetInRightDependency { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="TextModificationBase"/> class
        /// with the provided identifiers of start and end splices as well as the respective
        /// character offsets.
        /// </summary>
        /// <param name="leftDependencyId">Identifier of the splice where modification starts</param>
        /// <param name="offsetInLeftDependency">Character offset in the starting splice</param>
        /// <param name="rightDependencyId">Identifier of the splice where modification ends</param>
        /// <param name="offsetInRightDependency">Character offset in the ending splice</param>
        public TextModificationBase(
            SpliceId leftDependencyId,
            Point offsetInLeftDependency,
            SpliceId rightDependencyId,
            Point offsetInRightDependency)
        {
            LeftDependencyId = leftDependencyId;
            OffsetInLeftDependency = offsetInLeftDependency;
            RightDependencyId = rightDependencyId;
            OffsetInRightDependency = offsetInRightDependency;
        }
    }
}
