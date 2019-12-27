using System.Collections.Generic;

namespace Teletype.Contracts
{
    /// <summary>
    /// Defines set of linear updates to the document.
    /// </summary>
    public sealed class DocumentStateUpdate
    {
        /// <summary>
        /// Gets the collection of linear text updates.
        /// </summary>
        public List<TextUpdate> TextUpdates { get; }

        /// <summary>
        /// Gets the state of markers per site, per layer.
        /// </summary>
        /// <remarks>
        /// The mapping is as following: SiteId -> LayerId -> MarkerId -> Marker.
        /// </remarks>
        public Dictionary<int, Dictionary<int, Dictionary<int, Marker<Range>>>> MarkerUpdates { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="DocumentStateUpdate"/> class
        /// with the provided text and marker updates.
        /// </summary>
        /// <param name="textUpdates">Linear text updates</param>
        /// <param name="markerUpdates">Updates for document markers</param>
        public DocumentStateUpdate(
            List<TextUpdate> textUpdates,
            Dictionary<int, Dictionary<int, Dictionary<int, Marker<Range>>>> markerUpdates)
        {
            TextUpdates = textUpdates;
            MarkerUpdates = markerUpdates;
        }
    }
}
