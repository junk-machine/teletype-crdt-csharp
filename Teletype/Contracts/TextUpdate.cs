namespace Teletype.Contracts
{
    /// <summary>
    /// Defines linear text update within the document.
    /// </summary>
    public sealed class TextUpdate
    {
        /// <summary>
        /// Gets the start position of the text prior to the update.
        /// </summary>
        public Point OldStart { get; set; }

        /// <summary>
        /// Gets or sets the end position of the text prior to the update.
        /// </summary>
        public Point OldEnd { get; set; }

        /// <summary>
        /// Gets or sets the old text in the given range.
        /// </summary>
        public string OldText { get; set; }

        /// <summary>
        /// Gets or sets the start position of the text after the update.
        /// </summary>
        public Point NewStart { get; set; }

        /// <summary>
        /// Gets or sets the end position of the text after the update.
        /// </summary>
        public Point NewEnd { get; set; }

        /// <summary>
        /// Gets or sets the updated text for the given range.
        /// </summary>
        public string NewText { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="TextUpdate"/> class
        /// with the provided before and after state.
        /// </summary>
        /// <param name="oldStart">Start position of the text prior to the update</param>
        /// <param name="oldEnd">End position of the text prior to the update</param>
        /// <param name="oldText">Old text in the given range</param>
        /// <param name="newStart">Start position of the text after the update</param>
        /// <param name="newEnd">End position of the text after the update</param>
        /// <param name="newText">Updated text for the given range</param>
        public TextUpdate(
            Point oldStart,
            Point oldEnd,
            string oldText,
            Point newStart,
            Point newEnd,
            string newText)
        {
            OldStart = oldStart;
            OldEnd = oldEnd;
            OldText = oldText;
            NewStart = newStart;
            NewEnd = newEnd;
            NewText = newText;
        }
    }
}
