using System.Collections.Generic;
using Teletype.Contracts;

namespace Teletype
{
    /// <summary>
    /// Record of a checkpoint on the undo stack.
    /// </summary>
    internal sealed class CheckpointRecord : IUndoRecord
    {
        /// <summary>
        /// Gets the checkpoint identifier.
        /// </summary>
        public int Id { get; }

        /// <summary>
        /// Gets the flag indicating whether checkpoint acts as a barrier on the undo stack.
        /// </summary>
        public bool IsBarrier { get; }

        /// <summary>
        /// Gets the markers snapshot.
        /// </summary>
        public IReadOnlyDictionary<int, IReadOnlyDictionary<int, Marker<LogicalRange>>> MarkersSnapshot { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="CheckpointRecord"/> class
        /// with the provided checkpoint identifier, barrier flag and markers snapshot.
        /// </summary>
        /// <param name="id">Checkpoint identifier</param>
        /// <param name="isBarrier">Flag indicating whether checkpoint acts as a barrier on the undo stack.</param>
        /// <param name="markersSnapshot">Markers state</param>
        public CheckpointRecord(
            int id,
            bool isBarrier,
            IReadOnlyDictionary<int, IReadOnlyDictionary<int, Marker<LogicalRange>>> markersSnapshot)
        {
            Id = id;
            IsBarrier = isBarrier;
            MarkersSnapshot = markersSnapshot;
        }
    }
}
