namespace Teletype
{
    /// <summary>
    /// Base class for any splay tree.
    /// </summary>
    internal abstract class SplayTree
    {
        /// <summary>
        /// Gets the root segment of the tree.
        /// </summary>
        public Segment Root { get; protected set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="SplayTree"/> class
        /// with the provided root text segment.
        /// </summary>
        /// <param name="root">Root text segment</param>
        protected SplayTree(Segment root)
        {
            Root = root;
        }

        /// <summary>
        /// Gets the first segment to the right of the current.
        /// This algorithm considers that entire right tree is to the right,
        /// therefore first node to the right is the left-most node in the right subtree.
        /// If right subtree is empty, then first node would be a parent that is to the right
        /// of the given node (where node is the left child).
        /// </summary>
        /// <param name="segment">Segment to get a successor for</param>
        /// <returns>Successor of the given segment.</returns>
        public virtual Segment GetSuccessor(Segment segment)
        {
            if (GetRight(segment) != null)
            {
                segment = GetRight(segment);

                while (GetLeft(segment) != null)
                {
                    segment = GetLeft(segment);
                }
            }
            else
            {
                while (GetParent(segment) != null && GetRight(GetParent(segment)) == segment)
                {
                    segment = GetParent(segment);
                }

                segment = GetParent(segment);
            }

            return segment;
        }

        /// <summary>
        /// Reorganizes the tree so that <paramref name="node"/> becomes a skip-level parent, if there is enought depth,
        /// otherwise just rotates the tree to make it a parent.
        /// </summary>
        /// <param name="node">Node to promote to skip-level parent</param>
        /// <remarks>
        /// To better understand the logic, there are illustration in different code branches. The goal of the code is
        /// to make current node (+) to become the root of a subtree in all illustrated situation.
        /// </remarks>
        public void SplayNode(Segment node)
        {
            if (node == null)
            {
                return;
            }

            while (true)
            {
                // Determine relative position of the node within the tree
                if (IsNodeLeftChild(GetParent(node)) && IsNodeRightChild(node))
                {
                    /*
                         o          o          +
                        /          /          /
                       o    ->    +    ->    o
                        \        /          /
                         +      o          o
                    */
                    RotateNodeLeft(node);
                    RotateNodeRight(node);
                }
                else if (IsNodeRightChild(GetParent(node)) && IsNodeLeftChild(node))
                {
                    /*
                       o        o        +
                        \        \        \
                         o  ->    +  ->    o
                        /        /        /
                       +        o        o
                    */
                    RotateNodeRight(node);
                    RotateNodeLeft(node);
                }
                else if (IsNodeLeftChild(GetParent(node)) && IsNodeLeftChild(node))
                {
                    /*
                           o                 +
                          /         o         \
                         o    ->   / \   ->    o
                        /         +   o         \
                       +                         o
                    */
                    RotateNodeRight(GetParent(node));
                    RotateNodeRight(node);
                }
                else if (IsNodeRightChild(GetParent(node)) && IsNodeRightChild(node))
                {
                    /*
                       o                         +
                        \           o           /
                         o    ->   / \   ->    o
                          \       o   +       /
                           +                 o
                    */
                    RotateNodeLeft(GetParent(node));
                    RotateNodeLeft(node);
                }
                else
                {
                    
                    if (IsNodeLeftChild(node))
                    {
                        /*
                             o      +
                            /   ->   \
                           +          o
                        */
                        RotateNodeRight(node);
                    }
                    else if (IsNodeRightChild(node))
                    {
                        /*
                           o        +
                            \   ->   \
                             +        o
                        */
                        RotateNodeLeft(node);
                    }

                    return;
                }
            }
        }

        /// <summary>
        /// Re-calculates an extent for the given <paramref name="segment"/> based on its children.
        /// </summary>
        /// <param name="segment">Segment to update</param>
        public abstract void UpdateSubtreeExtent(Segment segment);

        /// <summary>
        /// Retrieves parent of the given <paramref name="segment"/>.
        /// </summary>
        /// <param name="segment">Segment to retrieve parent for</param>
        /// <returns>The parent of the segment.</returns>
        protected abstract Segment GetParent(Segment segment);

        /// <summary>
        /// Updates the parent of the given <paramref name="segment"/> with
        /// the given <paramref name="value"/>.
        /// </summary>
        /// <param name="segment">Segment to set parent for</param>
        /// <param name="value">New parent segment</param>
        protected abstract void SetParent(Segment segment, Segment value);

        /// <summary>
        /// Retrieves left child of the given <paramref name="segment"/>.
        /// </summary>
        /// <param name="segment">Segment to retrieve child for</param>
        /// <returns>Left child of the segment.</returns>
        protected abstract Segment GetLeft(Segment segment);

        /// <summary>
        /// Updates left child of the given <paramref name="segment"/> with
        /// the given <paramref name="value"/>.
        /// </summary>
        /// <param name="segment">Segment to set left child for</param>
        /// <param name="value">New left child segment</param>
        protected abstract void SetLeft(Segment segment, Segment value);

        /// <summary>
        /// Retrieves right child of the given <paramref name="segment"/>.
        /// </summary>
        /// <param name="segment">Segment to retrieve child for</param>
        /// <returns>Right child of the segment.</returns>
        protected abstract Segment GetRight(Segment segment);

        /// <summary>
        /// Updates right child of the given <paramref name="segment"/> with
        /// the given <paramref name="value"/>.
        /// </summary>
        /// <param name="segment">Segment to set right child for</param>
        /// <param name="value">New right child segment</param>
        protected abstract void SetRight(Segment segment, Segment value);

        /// <summary>
        /// Checks if given segment is a left child of its parent.
        /// </summary>
        /// <param name="segment">Segment to check</param>
        /// <returns>true if segment is a left child, otherwise false.</returns>
        private bool IsNodeLeftChild(Segment segment)
        {
            return segment != null && GetParent(segment) != null && GetLeft(GetParent(segment)) == segment;
        }

        /// <summary>
        /// Checks if given segment is a right child of its parent.
        /// </summary>
        /// <param name="segment">Segment to check</param>
        /// <returns>true if segment is a right child, otherwise false.</returns>
        private bool IsNodeRightChild(Segment segment)
        {
            return segment != null && GetParent(segment) != null && GetRight(GetParent(segment)) == segment;
        }

        /// <summary>
        /// Rotates a subtree under <paramref name="pivot"/>'s parent counter-clockwise,
        /// in such a way that <paramref name="pivot"/> becomes new parent of the subtree.
        /// </summary>
        /// <param name="pivot">Pivotal segment</param>
        private void RotateNodeLeft(Segment pivot)
        {
            var root = GetParent(pivot);
            if (GetParent(root) != null)
            {
                if (root == GetLeft(GetParent(root)))
                {
                    SetLeft(GetParent(root), pivot);
                }
                else
                {
                    SetRight(GetParent(root), pivot);
                }
            }
            else
            {
                Root = pivot;
            }

            SetParent(pivot, GetParent(root));

            SetRight(root, GetLeft(pivot));
            if (GetRight(root) != null)
            {
                SetParent(GetRight(root), root);
            }

            SetLeft(pivot, root);
            SetParent(GetLeft(pivot), pivot);

            UpdateSubtreeExtent(root);
            UpdateSubtreeExtent(pivot);
        }

        /// <summary>
        /// Rotates a subtree under <paramref name="pivot"/>'s parent clockwise,
        /// in such a way that <paramref name="pivot"/> becomes new parent of the subtree.
        /// </summary>
        /// <param name="pivot">Pivotal node</param>
        private void RotateNodeRight(Segment pivot)
        {
            var root = GetParent(pivot);
            if (GetParent(root) != null)
            {
                if (root == GetLeft(GetParent(root)))
                {
                    SetLeft(GetParent(root), pivot);
                }
                else
                {
                    SetRight(GetParent(root), pivot);
                }
            }
            else
            {
                Root = pivot;
            }

            SetParent(pivot, GetParent(root));

            SetLeft(root, GetRight(pivot));

            if (GetLeft(root) != null)
            {
                SetParent(GetLeft(root), root);
            }

            SetRight(pivot, root);
            SetParent(GetRight(pivot), pivot);

            UpdateSubtreeExtent(root);
            UpdateSubtreeExtent(pivot);
        }
    }
}
