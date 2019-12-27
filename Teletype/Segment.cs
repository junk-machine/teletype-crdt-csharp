using System;
using System.Collections.Generic;
using Teletype.Contracts;

namespace Teletype
{
    /// <summary>
    /// Defines text segment.
    /// </summary>
    /// <remarks>
    /// Due to optimizations, this structure also maintains two trees:
    ///  - Split tree
    ///  - Document tree
    /// It might be possible to break it down in to proper tree-node and an actual text segment,
    /// but there is a lot of original code that depends on the fact that these segments can be
    /// quickly found in the opposing tree, so that it will incur performance or memory impact.
    /// </remarks>
    internal sealed class Segment
    {
        /// <summary>
        /// Gets the identifier of the splice.
        /// </summary>
        public SpliceId SpliceId { get; }

        /// <summary>
        /// Gets or sets the segment offset.
        /// </summary>
        public Point Offset { get; set; }

        /// <summary>
        /// Gets or sets the segment text.
        /// </summary>
        public string Text { get; set; }

        /// <summary>
        /// Gets or sets the text extent.
        /// </summary>
        public Point Extent { get; set; }

        /// <summary>
        /// Gets the text deletions.
        /// </summary>
        public HashSet<SpliceId> Deletions { get; }

        /// <summary>
        /// Gets or sets the segment to the left of this segment within the document.
        /// </summary>
        public Segment LeftDependency { get; set; }

        /// <summary>
        /// Gets or sets the segment to the right of this segment within the document.
        /// </summary>
        public Segment RightDependency { get; set; }

        #region Split tree properties

        /// <summary>
        /// Left of the current node in a split tree.
        /// </summary>
        public Segment SplitLeft { get; set; }

        /// <summary>
        /// Right of the current node in a split tree.
        /// </summary>
        public Segment SplitRight { get; set; }

        /// <summary>
        /// Parent of the current node in a split tree.
        /// </summary>
        public Segment SplitParent { get; set; }

        /// <summary>
        /// Compound extent for the text stored in the entire subtree of the current node in a split tree.
        /// </summary>
        public Point SplitSubtreeExtent { get; set; }

        /// <summary>
        /// Next segment in the split tree in linear order.
        /// </summary>
        /// <remarks>
        /// This is alternative linked-list data structure that allows to quickly iterate
        /// over all segments in linear order (of appearance in the document).
        /// </remarks>
        public Segment NextSplit { get; set; }

        #endregion Split tree properties

        #region Document tree properties

        /// <summary>
        /// Left of the current node in a document tree.
        /// </summary>
        public Segment DocumentLeft { get; set; }

        /// <summary>
        /// Right of the current node in a document tree.
        /// </summary>
        public Segment DocumentRight { get; set; }

        /// <summary>
        /// Parent of the current node in a document tree.
        /// </summary>
        public Segment DocumentParent { get; set; }

        /// <summary>
        /// Compound extent for the text stored in the entire subtree of the current node in a document tree.
        /// </summary>
        public Point DocumentSubtreeExtent { get; set; }

        /// <summary>
        /// Gets or sets the number of nodes in the subtree in a document tree.
        /// </summary>
        public int DocumentSubtreeSize { get; set; }

        #endregion Document tree properties

        /// <summary>
        /// Initializes a new instance of the <see cref="Segment"/> class
        /// with the provided splice identifier, offset, text and extent.
        /// </summary>
        /// <param name="spliceId">Splice identifier</param>
        /// <param name="offset">Text offset</param>
        /// <param name="text">Segment text</param>
        /// <param name="extent">Text extent</param>
        public Segment(SpliceId spliceId, Point offset, string text, Point extent)
            : this(spliceId, offset, text, extent, new HashSet<SpliceId>()) { }

        /// <summary>
        /// Initializes a new instance of the <see cref="Segment"/> class
        /// with the provided splice identifier, offset, text, extent and deletions.
        /// </summary>
        /// <param name="spliceId">Splice identifier</param>
        /// <param name="offset">Text offset</param>
        /// <param name="text">Segment text</param>
        /// <param name="extent">Text extent</param>
        /// <param name="deletions">Text deletions</param>
        public Segment(SpliceId spliceId, Point offset, string text, Point extent, HashSet<SpliceId> deletions)
        {
            SpliceId = spliceId;
            Offset = offset;
            Text = text;
            Extent = extent;
            Deletions = deletions ?? throw new ArgumentNullException(nameof(deletions));
        }

        /// <summary>
        /// Formats splice identifier and text of the segment as a string.
        /// </summary>
        /// <returns>Splice identifier and text of the segment.</returns>
        public override string ToString()
        {
            return $"{SpliceId} \"{Text}\"";
        }
    }
}
