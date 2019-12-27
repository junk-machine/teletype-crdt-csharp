namespace Teletype.Contracts
{
    /// <summary>
    /// Identifies linear range in the text document.
    /// </summary>
    public sealed class Range
    {
        /// <summary>
        /// Gets the starting positon of the range.
        /// </summary>
        public Point Start { get; }

        /// <summary>
        /// Gets the ending position of the range.
        /// </summary>
        public Point End { get; }

        /// <summary>
        /// Initializes a new isntance of the <see cref="Range"/> class
        /// with the provided start and end positions.
        /// </summary>
        /// <param name="start">Point in the document where range starts</param>
        /// <param name="end">Point in the document where range ends</param>
        public Range(Point start, Point end)
        {
            Start = start;
            End = end;
        }
    }
}
