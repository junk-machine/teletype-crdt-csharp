using System;
using System.Collections.Generic;
using Teletype.Contracts;

namespace Teletype
{
    /// <summary>
    /// Record of a transaction on the undo stack.
    /// </summary>
    internal sealed class TransactionRecord : IUndoRecord
    {
        /// <summary>
        /// Gets or sets the transaction time.
        /// </summary>
        public long Timestamp { get; set; }

        /// <summary>
        /// Gets or sets the grouping interval of this transaction.
        /// </summary>
        public long? GroupingInterval { get; set; }

        /// <summary>
        /// Gets all operations within transaction.
        /// </summary>
        public List<IOperation> Operations { get; }

        /// <summary>
        /// Gets or sets the markers state before the transaction.
        /// </summary>
        public IReadOnlyDictionary<int, IReadOnlyDictionary<int, Marker<LogicalRange>>> MarkersSnapshotBefore { get; set; }

        /// <summary>
        /// Gets or sets the markers state after the transaction.
        /// </summary>
        public IReadOnlyDictionary<int, IReadOnlyDictionary<int, Marker<LogicalRange>>> MarkersSnapshotAfter { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="TransactionRecord"/> class
        /// with the provided timestamp and operations.
        /// </summary>
        /// <param name="timestamp">Transaction time</param>
        /// <param name="operations">Operations within the transaction</param>
        public TransactionRecord(long timestamp, List<IOperation> operations)
            : this(timestamp, operations, null, null)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="TransactionRecord"/> class
        /// with the provided timestamp, operations and markers snapshots.
        /// </summary>
        /// <param name="timestamp">Transaction time</param>
        /// <param name="operations">Operations within the transaction</param>
        /// <param name="markersSnapshotBefore">Markers state before the transaction</param>
        /// <param name="markersSnapshotAfter">Markers state after the transaction</param>
        public TransactionRecord(
            long timestamp,
            List<IOperation> operations,
            IReadOnlyDictionary<int, IReadOnlyDictionary<int, Marker<LogicalRange>>> markersSnapshotBefore,
            IReadOnlyDictionary<int, IReadOnlyDictionary<int, Marker<LogicalRange>>> markersSnapshotAfter)
        {
            Timestamp = timestamp;
            Operations = operations;
            MarkersSnapshotBefore = markersSnapshotBefore;
            MarkersSnapshotAfter = markersSnapshotAfter;
        }
    }
}
