using System;
using System.Collections.Generic;
using Teletype.Contracts;
using Teletype.Properties;

namespace Teletype
{
    /// <summary>
    /// Maintains tree of text segments that together constitute full document.
    /// </summary>
    internal sealed class DocumentTree : SplayTree
    {
        /// <summary>
        /// The first text segment in the document.
        /// </summary>
        private readonly Segment first;

        /// <summary>
        /// Delegate to check whether text segment is visible in the document.
        /// </summary>
        private readonly Func<Segment, Dictionary<SpliceId, int>, HashSet<SpliceId>, bool> isSegmentVisible;

        /// <summary>
        /// Initializes a new instance of the <see cref="DocumentTree"/> class
        /// with the provided first and last text segments, and a delegate
        /// to check whether text segment is visible in the document.
        /// </summary>
        /// <param name="firstSegment">First document text segment</param>
        /// <param name="lastSegment">Last document text segment</param>
        /// <param name="isSegmentVisible">Delegate to check whether text segment is visible in the document</param>
        public DocumentTree(
            Segment firstSegment,
            Segment lastSegment,
            Func<Segment, Dictionary<SpliceId, int>, HashSet<SpliceId>, bool> isSegmentVisible)
                : base(firstSegment ?? throw new ArgumentNullException(nameof(firstSegment)))
        {
            first = firstSegment;
            var last = lastSegment ?? throw new ArgumentNullException(nameof(lastSegment));

            first.DocumentRight = last;
            last.DocumentParent = first;

            this.isSegmentVisible = isSegmentVisible;
        }

        /// <summary>
        /// Computes the linear index of the text segment.
        /// All segments to the left (in the left sub-tree) are considered before the current node.
        /// </summary>
        /// <param name="segment">Text segment</param>
        /// <returns>Linear index of the text segment.</returns>
        public int GetSegmentIndex(Segment segment)
        {
            var index = segment.DocumentLeft != null ? segment.DocumentLeft.DocumentSubtreeSize : 0;

            while (segment.DocumentParent != null)
            {
                if (segment.DocumentParent.DocumentRight == segment)
                {
                    ++index;

                    if (segment.DocumentParent.DocumentLeft != null)
                    {
                        index += segment.DocumentParent.DocumentLeft.DocumentSubtreeSize;
                    }
                }

                segment = segment.DocumentParent;
            }

            return index;
        }

        /// <summary>
        /// Looks up a text segment containing given <paramref name="position"/>.
        /// </summary>
        /// <param name="position">Position to look up</param>
        /// <returns>Text segment where given <paramref name="position"/> falls into.</returns>
        public Tuple<Segment, Point, Point> FindSegmentContainingPosition(Point position)
        {
            var segment = Root;
            var leftAncestorEnd = Point.Zero;

            while (segment != null)
            {
                var start = leftAncestorEnd;
                if (segment.DocumentLeft != null)
                {
                    start = start.Traverse(segment.DocumentLeft.DocumentSubtreeExtent);
                }

                var end = start;

                if (isSegmentVisible(segment, null, null))
                {
                    end = end.Traverse(segment.Extent);
                }

                if (position.CompareTo(start) <= 0 && segment != first)
                {
                    segment = segment.DocumentLeft;
                }
                else if (position.CompareTo(end) > 0)
                {
                    leftAncestorEnd = end;
                    segment = segment.DocumentRight;
                }
                else
                {
                    return Tuple.Create(segment, start, end);
                }
            }

            throw new IndexOutOfRangeException(string.Format(Resources.SegmentNotFoundErrorFormat, position));
        }

        /// <summary>
        /// Inserts new text segment between <paramref name="prev"/> and <paramref name="next"/>.
        /// </summary>
        /// <param name="prev">Text segment after which insertion should happen</param>
        /// <param name="next">Text segment before which insertion should happen</param>
        /// <param name="newSegment">New text segment to insert</param>
        public void InsertBetween(Segment prev, Segment next, Segment newSegment)
        {
            SplayNode(prev);
            SplayNode(next);
            Root = newSegment;
            newSegment.DocumentLeft = prev;
            prev.DocumentParent = newSegment;
            newSegment.DocumentRight = next;
            next.DocumentParent = newSegment;
            next.DocumentLeft = null;
            UpdateSubtreeExtent(next);
            UpdateSubtreeExtent(newSegment);
        }

        /// <summary>
        /// Replaces one segment with <paramref name="prefix"/> and <paramref name="suffix"/>.
        /// </summary>
        /// <param name="prefix">First segment</param>
        /// <param name="suffix">Second segment</param>
        public void SplitSegment(Segment prefix, Segment suffix)
        {
            SplayNode(prefix);

            Root = suffix;
            suffix.DocumentParent = null;
            suffix.DocumentLeft = prefix;
            prefix.DocumentParent = suffix;
            suffix.DocumentRight = prefix.DocumentRight;
            if (suffix.DocumentRight != null)
            {
                suffix.DocumentRight.DocumentParent = suffix;
            }

            prefix.DocumentRight = null;

            UpdateSubtreeExtent(prefix);
            UpdateSubtreeExtent(suffix);
        }

        /// <summary>
        /// Gets the start position of the given text segment.
        /// </summary>
        /// <param name="segment">Text segment</param>
        /// <returns>The start position of the text segment.</returns>
        public Point GetSegmentPosition(Segment segment)
        {
            SplayNode(segment);

            return segment.DocumentLeft != null
                ? segment.DocumentLeft.DocumentSubtreeExtent
                : Point.Zero;
        }

        /// <summary>
        /// Returns text segments by performing in-order document tree traversal.
        /// </summary>
        /// <returns>All document text segments.</returns>
        public IEnumerable<Segment> GetSegments()
        {
            // TODO: Verify this algorithm: non-recursive in-order (L-N-R) traversal
            var segment = Root;
            var needsLeftTraversal = true;

            while (segment != null)
            {
                if (needsLeftTraversal)
                {
                    while (segment.DocumentLeft != null)
                    {
                        segment = segment.DocumentLeft;
                    }
                }
                else
                {
                    needsLeftTraversal = true;
                }

                yield return segment;
                
                if (segment.DocumentRight != null)
                {
                    segment = segment.DocumentRight;
                }
                else
                {
                    // Returning from the right branch, parent nodes to the left were already visited.
                    // Need to unwind to the nearest parent on the right (to which we are the left node),
                    // so that it will be visited and another right traversal can begin.
                    while (segment.DocumentParent != null && segment != segment.DocumentParent.DocumentLeft)
                    {
                        segment = segment.DocumentParent;
                    }

                    segment = segment.DocumentParent;
                    needsLeftTraversal = false;
                }
            }
        }

        /// <summary>
        /// Re-calculates an extent and subtree size for the <paramref name="segment"/> based on its children.
        /// </summary>
        /// <param name="segment">Text segment to update</param>
        public override void UpdateSubtreeExtent(Segment segment)
        {
            UpdateSubtreeExtent(segment, null);
        }

        /// <summary>
        /// Re-calculates an extent and subtree size for the <paramref name="segment"/> based on its children.
        /// </summary>
        /// <param name="segment">Text segment to update</param>
        /// <param name="undoCountOverrides">Overrides for number of undo operations</param>
        private void UpdateSubtreeExtent(Segment segment, Dictionary<SpliceId, int> undoCountOverrides)
        {
            segment.DocumentSubtreeExtent = Point.Zero;
            segment.DocumentSubtreeSize = 1;

            if (segment.DocumentLeft != null)
            {
                segment.DocumentSubtreeExtent = segment.DocumentSubtreeExtent.Traverse(segment.DocumentLeft.DocumentSubtreeExtent);
                segment.DocumentSubtreeSize += segment.DocumentLeft.DocumentSubtreeSize;
            }

            if (isSegmentVisible(segment, undoCountOverrides, null))
            {
                segment.DocumentSubtreeExtent = segment.DocumentSubtreeExtent.Traverse(segment.Extent);
            }

            if (segment.DocumentRight != null)
            {
                segment.DocumentSubtreeExtent = segment.DocumentSubtreeExtent.Traverse(segment.DocumentRight.DocumentSubtreeExtent);
                segment.DocumentSubtreeSize += segment.DocumentRight.DocumentSubtreeSize;
            }
        }

        /// <summary>
        /// Retrieves <see cref="Segment.DocumentParent"/> property value for the given <paramref name="segment"/>.
        /// </summary>
        /// <param name="segment">Segment to retrieve parent for</param>
        /// <returns>Parent of the segment in a document tree.</returns>
        protected override Segment GetParent(Segment segment)
        {
            return segment.DocumentParent;
        }

        /// <summary>
        /// Updates <see cref="Segment.DocumentParent"/> property with the given <paramref name="value"/>.
        /// </summary>
        /// <param name="segment">Segment to set parent for</param>
        /// <param name="value">New parent for the segment in a document tree.</param>
        protected override void SetParent(Segment segment, Segment value)
        {
            segment.DocumentParent = value;
        }

        /// <summary>
        /// Retrieves <see cref="Segment.DocumentLeft"/> property value for the given <paramref name="segment"/>.
        /// </summary>
        /// <param name="segment">Segment to retrieve left child for</param>
        /// <returns>Left child of the segment in a document tree.</returns>
        protected override Segment GetLeft(Segment segment)
        {
            return segment.DocumentLeft;
        }

        /// <summary>
        /// Updates <see cref="Segment.DocumentLeft"/> property with the given <paramref name="value"/>.
        /// </summary>
        /// <param name="segment">Segment to set left child for</param>
        /// <param name="value">Left child of the segment in a document tree.</param>
        protected override void SetLeft(Segment segment, Segment value)
        {
            segment.DocumentLeft = value;
        }

        /// <summary>
        /// Retrieves <see cref="Segment.DocumentRight"/> property value for the given <paramref name="segment"/>.
        /// </summary>
        /// <param name="segment">Segment to retrieve right child for</param>
        /// <returns>Right child of the segment in a document tree.</returns>
        protected override Segment GetRight(Segment segment)
        {
            return segment.DocumentRight;
        }

        /// <summary>
        /// Updates <see cref="Segment.DocumentRight"/> property with the given <paramref name="value"/>.
        /// </summary>
        /// <param name="segment">Segment to set right child for</param>
        /// <param name="value">Right child of the segment in a document tree.</param>
        protected override void SetRight(Segment segment, Segment value)
        {
            segment.DocumentRight = value;
        }
    }
}
