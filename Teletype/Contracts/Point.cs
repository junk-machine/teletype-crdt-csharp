using System;
using Teletype.Properties;

namespace Teletype
{
    /// <summary>
    /// Identifies character location within the document.
    /// </summary>
    public struct Point : IComparable<Point>
    {
        /// <summary>
        /// Location that points to the begining of the document (row 0, column 0).
        /// </summary>
        public static readonly Point Zero = new Point();

        /// <summary>
        /// Row number.
        /// </summary>
        public readonly int Row;

        /// <summary>
        /// Column number.
        /// </summary>
        public readonly int Column;

        /// <summary>
        /// Initializes a <see cref="Point"/> structure
        /// with the given row and column numbers.
        /// </summary>
        /// <param name="row">Row number</param>
        /// <param name="column">Column number</param>
        public Point(int row, int column)
        {
            Row = row;
            Column = column;
        }

        /// <summary>
        /// Adjusts current location by a given <paramref name="distance"/>.
        /// </summary>
        /// <param name="distance">Distance to add to the current location</param>
        /// <returns>New point that is adjusted forward by a given <paramref name="distance"/>.</returns>
        public Point Traverse(Point distance)
        {
            return distance.Row == 0
                ? new Point(Row, Column + distance.Column)
                : new Point(Row + distance.Row, distance.Column);
        }

        /// <summary>
        /// Compares current character location to another.
        /// </summary>
        /// <param name="other">Another character location</param>
        /// <returns>
        /// Negative value, if current character location is before the other one;
        /// positive value, if current character location is after the other one;
        /// zero if both point to the same location.</returns>
        public int CompareTo(Point other)
        {
            return Compare(Row, Column, other.Row, other.Column);
        }

        /// <summary>
        /// Generates the string representation of the current location.
        /// </summary>
        /// <returns>The string representation of the current location.</returns>
        public override string ToString()
        {
            return $"({Row}, {Column})";
        }

        /// <summary>
        /// Measures the distance between <paramref name="start"/> and <paramref name="end"/> locations.
        /// </summary>
        /// <param name="end">End location</param>
        /// <param name="start">Start location</param>
        /// <returns>Distance between <paramref name="start"/> and <paramref name="end"/>.</returns>
        public static Point Traversal(Point end, Point start)
        {
            return end.Row == start.Row
                ? new Point(0, end.Column - start.Column)
                : new Point(end.Row - start.Row, end.Column);
        }

        /// <summary>
        /// Computes an extent length for the text.
        /// </summary>
        /// <param name="text">Text data to compute an extent for</param>
        /// <returns>
        /// A <see cref="Point"/> value that contains position of the last character for the text,
        /// if it was to start at (0, 0).
        /// </returns>
        public static Point GetExtentForText(string text)
        {
            int row = 0;
            int column = 0;

            for (var index = 0; index < text.Length; ++index)
            {
                if (text[index] == '\n')
                {
                    column = 0;
                    ++row;
                }
                else
                {
                    ++column;
                }
            }

            return new Point(row, column);
        }

        /// <summary>
        /// Computes an index of the character in <paramref name="text"/>
        /// for a given <paramref name="position"/>.
        /// </summary>
        /// <param name="text">Input text</param>
        /// <param name="position">Character position</param>
        /// <returns>Character index in the input <paramref name="text"/>.</returns>
        public static int CharacterIndexForPosition(string text, Point position)
        {
            int row = 0;
            int column = 0;
            int index = 0;

            for (; index <= text.Length && Compare(row, column, position.Row, position.Column) < 0; ++index)
            {
                if (text[index] == '\n')
                {
                    column = 0;
                    ++row;
                }
                else
                {
                    ++column;
                }
            }

            if (Compare(row, column, position.Row, position.Column) > 0)
            {
                throw new IndexOutOfRangeException(Resources.PositionOutOfRangeError);
            }

            return index;
        }

        /// <summary>
        /// Compares two character locations.
        /// </summary>
        /// <param name="rowA">Row number of the first location</param>
        /// <param name="columnA">Column index of the first location</param>
        /// <param name="rowB">Row number of the second location</param>
        /// <param name="columnB">Column index of the second location</param>
        /// <returns></returns>
        private static int Compare(int rowA, int columnA, int rowB, int columnB)
        {
            return rowA == rowB ? columnA - columnB : rowA - rowB;
        }
    }
}
