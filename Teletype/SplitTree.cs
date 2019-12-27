using System;
using System.Collections.Generic;
using Teletype.Contracts;
using Teletype.Properties;

namespace Teletype
{
    /// <summary>
    /// Maintains a tree of split segments.
    /// </summary>
    internal sealed class SplitTree : SplayTree
    {
        /// <summary>
        /// Gets the first text segment in the split tree.
        /// </summary>
        public Segment First { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="SplitTree"/> class
        /// with the provided root text segment.
        /// </summary>
        /// <param name="root">Root text segment</param>
        public SplitTree(Segment root)
            : base(root ?? throw new ArgumentNullException(nameof(root)))
        {
            First = root;
            root.SplitSubtreeExtent = root.Extent;
        }

        /// <summary>
        /// Finds the text segment where given <paramref name="offset"/> falls into.
        /// </summary>
        /// <param name="offset">Text offset</param>
        /// <returns>The text segment for the given <paramref name="offset"/>.</returns>
        public Segment FindSegmentContainingOffset(Point offset)
        {
            var currentNode = Root;
            var leftAncestorEnd = Point.Zero;

            while (currentNode != null)
            {
                var start = leftAncestorEnd;
                if (GetLeft(currentNode) != null)
                {
                    start = start.Traverse(GetLeft(currentNode).SplitSubtreeExtent);
                }

                var end = start.Traverse(currentNode.Extent);

                if (offset.CompareTo(start) <= 0 && GetLeft(currentNode) != null)
                {
                    currentNode = GetLeft(currentNode);
                }
                else if (offset.CompareTo(end) > 0)
                {
                    leftAncestorEnd = end;
                    currentNode = GetRight(currentNode);
                }
                else
                {
                    SplayNode(currentNode);
                    return currentNode;
                }
            }

            throw new IndexOutOfRangeException(
                string.Format(Resources.SegmentNotFoundErrorFormat, offset));
        }

        /// <summary>
        /// Splits the given text <paramref name="segment"/> at the specified <paramref name="offset"/>
        /// and re-balances the tree.
        /// </summary>
        /// <param name="segment">Segment to split</param>
        /// <param name="offset">Split offset</param>
        public Segment SplitSegment(Segment segment, Point offset)
        {
            var splitIndex = Point.CharacterIndexForPosition(segment.Text, offset);

            SplayNode(segment);

            var suffix =
                new Segment(
                    segment.SpliceId,
                    segment.Offset.Traverse(offset),
                    segment.Text.Substring(splitIndex),
                    Point.Traversal(segment.Extent, offset),
                    new HashSet<SpliceId>(segment.Deletions));
            suffix.NextSplit = segment.NextSplit;

            segment.Text = segment.Text.Substring(0, splitIndex);
            segment.Extent = offset;
            segment.NextSplit = suffix;

            Root = suffix;
            suffix.SplitParent = null;
            suffix.SplitLeft = segment;
            segment.SplitParent = suffix;
            suffix.SplitRight = segment.SplitRight;
            if (suffix.SplitRight != null)
            {
                suffix.SplitRight.SplitParent = suffix;
            }

            segment.SplitRight = null;

            UpdateSubtreeExtent(segment);
            UpdateSubtreeExtent(suffix);

            return suffix;
        }

        /// <summary>
        /// Gets the next linear segment.
        /// </summary>
        /// <param name="segment">Current segment</param>
        /// <returns>Next linear segment.</returns>
        public override Segment GetSuccessor(Segment segment)
        {
            return segment.NextSplit;
        }

        /// <summary>
        /// Retrieves all text segments in the order of their appearance
        /// within the document.
        /// </summary>
        /// <returns>All text segments within the tree.</returns>
        public IEnumerable<Segment> GetSegments()
        {
            var segment = First;

            while (segment != null)
            {
                yield return segment;
                segment = segment.NextSplit;
            }
        }

        /// <summary>
        /// Re-calculates an extent for the given <paramref name="segment"/> based on its children.
        /// </summary>
        /// <param name="segment">Segment to update</param>
        public override void UpdateSubtreeExtent(Segment segment)
        {
            segment.SplitSubtreeExtent = Point.Zero;

            if (segment.SplitLeft != null)
            {
                segment.SplitSubtreeExtent = segment.SplitSubtreeExtent.Traverse(segment.SplitLeft.SplitSubtreeExtent);
            }

            segment.SplitSubtreeExtent = segment.SplitSubtreeExtent.Traverse(segment.Extent);

            if (segment.SplitRight != null)
            {
                segment.SplitSubtreeExtent = segment.SplitSubtreeExtent.Traverse(segment.SplitRight.SplitSubtreeExtent);
            }
        }

        /// <summary>
        /// Retrieves <see cref="Segment.SplitParent"/> property value for the given <paramref name="segment"/>.
        /// </summary>
        /// <param name="segment">Segment to retrieve parent for</param>
        /// <returns>Parent of the segment in a split tree.</returns>
        protected override Segment GetParent(Segment segment)
        {
            return segment.SplitParent;
        }

        /// <summary>
        /// Updates <see cref="Segment.SplitParent"/> property with the given <paramref name="value"/>.
        /// </summary>
        /// <param name="segment">Segment to set parent for</param>
        /// <param name="value">New parent for the segment in a split tree.</param>
        protected override void SetParent(Segment segment, Segment value)
        {
            segment.SplitParent = value;
        }

        /// <summary>
        /// Retrieves <see cref="Segment.SplitLeft"/> property value for the given <paramref name="segment"/>.
        /// </summary>
        /// <param name="segment">Segment to retrieve left child for</param>
        /// <returns>Left child of the segment in a split tree.</returns>
        protected override Segment GetLeft(Segment segment)
        {
            return segment.SplitLeft;
        }

        /// <summary>
        /// Updates <see cref="Segment.SplitLeft"/> property with the given <paramref name="value"/>.
        /// </summary>
        /// <param name="segment">Segment to set left child for</param>
        /// <param name="value">Left child of the segment in a split tree.</param>
        protected override void SetLeft(Segment segment, Segment value)
        {
            segment.SplitLeft = value;
        }

        /// <summary>
        /// Retrieves <see cref="Segment.SplitRight"/> property value for the given <paramref name="segment"/>.
        /// </summary>
        /// <param name="segment">Segment to retrieve right child for</param>
        /// <returns>Right child of the segment in a split tree.</returns>
        protected override Segment GetRight(Segment segment)
        {
            return segment.SplitRight;
        }

        /// <summary>
        /// Updates <see cref="Segment.SplitRight"/> property with the given <paramref name="value"/>.
        /// </summary>
        /// <param name="segment">Segment to set right child for</param>
        /// <param name="value">Right child of the segment in a split tree.</param>
        protected override void SetRight(Segment segment, Segment value)
        {
            segment.SplitRight = value;
        }
    }
}
