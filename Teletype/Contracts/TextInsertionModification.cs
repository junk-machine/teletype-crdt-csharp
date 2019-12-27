namespace Teletype.Contracts
{
    /// <summary>
    /// Document modification where text is being inserted.
    /// </summary>
    public class TextInsertionModification : TextModificationBase
    {
        /// <summary>
        /// Gets the inserted text.
        /// </summary>
        public string Text { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="TextInsertionModification"/> class
        /// with the provided text, identifiers of start and end splices as well as the respective
        /// character offsets.
        /// </summary>
        /// <param name="text">Inserted text</param>
        /// <param name="leftDependencyId">Identifier of the splice where modification starts</param>
        /// <param name="offsetInLeftDependency">Character offset in the starting splice</param>
        /// <param name="rightDependencyId">Identifier of the splice where modification ends</param>
        /// <param name="offsetInRightDependency">Character offset in the ending splice</param>
        public TextInsertionModification(
            string text,
            SpliceId leftDependencyId,
            Point offsetInLeftDependency,
            SpliceId rightDependencyId,
            Point offsetInRightDependency)
            : base(leftDependencyId, offsetInLeftDependency, rightDependencyId, offsetInRightDependency)
        {
            Text = text;
        }
    }
}
