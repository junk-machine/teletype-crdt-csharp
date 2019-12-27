namespace Teletype.Contracts
{
    /// <summary>
    /// Identifies range in the text document.
    /// </summary>
    public sealed class LogicalRange
    {
        /// <summary>
        /// Gets the identifier of the splice where range starts.
        /// </summary>
        public SpliceId StartDependencyId { get; }

        /// <summary>
        /// Gets the character offset in the splice where range starts.
        /// </summary>
        public Point OffsetInStartDependency { get; }

        /// <summary>
        /// Gets the identifier of the splice where range ends.
        /// </summary>
        public SpliceId EndDependencyId { get; }

        /// <summary>
        /// Gets the character offset in the splice where range ends.
        /// </summary>
        public Point OffsetInEndDependency { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="LogicalRange"/> class
        /// with the provided start and end logical position.
        /// </summary>
        /// <param name="startDependencyId">Identifier of the splice where range starts</param>
        /// <param name="offsetInStartDependency">Character offset in the splice where range starts</param>
        /// <param name="endDependencyId">Identifier of the splice where range ends</param>
        /// <param name="offsetInEndDependency">Character offset in the splice where range ends</param>
        public LogicalRange(
            SpliceId startDependencyId,
            Point offsetInStartDependency,
            SpliceId endDependencyId,
            Point offsetInEndDependency)
        {
            StartDependencyId = startDependencyId;
            OffsetInStartDependency = offsetInStartDependency;
            EndDependencyId = endDependencyId;
            OffsetInEndDependency = offsetInEndDependency;
        }
    }
}
