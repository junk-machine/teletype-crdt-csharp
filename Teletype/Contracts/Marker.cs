namespace Teletype.Contracts
{
    /// <summary>
    /// Defines a text selection.
    /// </summary>
    public sealed class Marker<TRange>
        where TRange : class
    {
        /// <summary>
        /// Gets the flag that indicates whether selection is exclusive.
        /// </summary>
        public bool Exclusive { get; }

        /// <summary>
        /// Gets the flag that indicates whether selection is reversed.
        /// </summary>
        public bool Reversed { get; }
        
        /// <summary>
        /// Gets the flag that indicates whether selection is tailed.
        /// </summary>
        public bool Tailed { get; }

        /// <summary>
        /// Gets the selection range.
        /// </summary>
        public TRange Range { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="Marker"/> class
        /// with the provided exclusive, reversed and tailed flags, as well as
        /// selection range.
        /// </summary>
        /// <param name="exclusive">Flag that indicates whether selection is exclusive</param>
        /// <param name="reversed">Flag that indicates whether selection is reversed</param>
        /// <param name="tailed">Flag that indicates whether selection is tailed</param>
        /// <param name="range">Selection range</param>
        public Marker(bool exclusive, bool reversed, bool tailed, TRange range)
        {
            Exclusive = exclusive;
            Reversed = reversed;
            Tailed = tailed;
            Range = range;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Marker"/> class
        /// with the default exclusive, reversed, tailed flags and provided
        /// selection range.
        /// </summary>
        /// <param name="range">Selection range</param>
        public Marker(TRange range)
            : this(false, false, true, range)
        {
        }
    }
}
