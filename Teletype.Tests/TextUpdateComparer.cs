using System.Collections.Generic;
using Teletype.Contracts;

namespace Teletype.Tests
{
    /// <summary>
    /// Equality comparer implementation for <see cref="TextUpdate"/> class.
    /// </summary>
    internal sealed class TextUpdateComparer : Comparer<TextUpdate>
    {
        /// <summary>
        /// Singleton instance of the comparer.
        /// </summary>
        public static readonly TextUpdateComparer Instance = new TextUpdateComparer();

        /// <summary>
        /// Compares two instances of <see cref="TextUpdate"/> class.
        /// </summary>
        /// <param name="x">First instance</param>
        /// <param name="y">Second instance</param>
        /// <returns>Zero if instances are the same, otherwise 1.</returns>
        public override int Compare(TextUpdate x, TextUpdate y)
        {
            return x.OldStart.CompareTo(y.OldStart) == 0
                && x.OldEnd.CompareTo(y.OldEnd) == 0
                && x.OldText == y.OldText
                && x.NewStart.CompareTo(y.NewStart) == 0
                && x.NewEnd.CompareTo(y.NewEnd) == 0
                && x.NewText == y.NewText ? 0 : 1;
        }
    }
}
