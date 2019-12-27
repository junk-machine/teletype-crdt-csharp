using System.Collections.Generic;

namespace Teletype.Contracts
{
    /// <summary>
    /// Record of a transaction in the history undo stack.
    /// </summary>
    public sealed class TransactionHistoryRecord : IUndoHistoryRecord
    {
        /// <summary>
        /// Gets the text changes within the transaction.
        /// </summary>
        public IReadOnlyList<TextUpdate> Changes { get; }

        /// <summary>
        /// Gets the markers state before the transaction.
        /// </summary>
        public IReadOnlyDictionary<int, IReadOnlyDictionary<int, Marker<Range>>> MarkersBefore { get; }

        /// <summary>
        /// Gets the markers state after the transaction.
        /// </summary>
        public IReadOnlyDictionary<int, IReadOnlyDictionary<int, Marker<Range>>> MarkersAfter { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="TransactionRecord"/> class
        /// with the provided changes and markers snapshots.
        /// </summary>
        /// <param name="changes">Changes within the transaction</param>
        /// <param name="markersBefore">Markers state before the transaction</param>
        /// <param name="markersAfter">Markers state after the transaction</param>
        public TransactionHistoryRecord(
            IReadOnlyList<TextUpdate> changes,
            IReadOnlyDictionary<int, IReadOnlyDictionary<int, Marker<Range>>> markersBefore,
            IReadOnlyDictionary<int, IReadOnlyDictionary<int, Marker<Range>>> markersAfter)
        {
            Changes = changes;
            MarkersBefore = markersBefore;
            MarkersAfter = markersAfter;
        }
    }
}
