using System.Collections.Generic;

namespace Teletype.Contracts
{
    /// <summary>
    /// Record of a checkpoint in the history undo stack.
    /// </summary>
    public sealed class CheckpointHistoryRecord : IUndoHistoryRecord
    {
        /// <summary>
        /// Gets the checkpoint identifier.
        /// </summary>
        public int Id { get; }

        /// <summary>
        /// Gets the markers snapshot.
        /// </summary>
        public IReadOnlyDictionary<int, IReadOnlyDictionary<int, Marker<Range>>> Markers { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="CheckpointHistoryRecord"/> class
        /// with the provided checkpoint identifier and markers snapshot.
        /// </summary>
        /// <param name="id">Checkpoint identifier</param>
        /// <param name="markers">Markers state</param>
        public CheckpointHistoryRecord(
            int id,
            IReadOnlyDictionary<int, IReadOnlyDictionary<int, Marker<Range>>> markers)
        {
            Id = id;
            Markers = markers;
        }
    }
}
