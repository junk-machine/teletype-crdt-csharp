using Teletype.Contracts;

namespace Teletype
{
    /// <summary>
    /// Defines selection change for a given site, layer and marker.
    /// </summary>
    /// <remarks>
    /// This operation is used internally to buffer marker updates that cannot be applied at the moment
    /// due to missing dependency. This is just the pointer to the deferred marker update, actual marker
    /// information is stored separately.
    /// </remarks>
    internal sealed class DeferredMarkerUpdateOperation : IOperation
    {
        /// <summary>
        /// Gets the identifier of the site.
        /// </summary>
        public int SiteId { get; }

        /// <summary>
        /// Gets the identifier of markers layer.
        /// </summary>
        public int LayerId { get; }

        /// <summary>
        /// Gets the identifier of the marker
        /// </summary>
        public int MarkerId { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="DeferredMarkerUpdateOperation"/> class
        /// with the provided site identifier, layer identifier, marker identifier and update marker.
        /// </summary>
        /// <param name="siteId">Site identifier</param>
        /// <param name="layerId">Markers layer identifier</param>
        /// <param name="markerId">Marker identifier</param>
        public DeferredMarkerUpdateOperation(int siteId, int layerId, int markerId)
        {
            SiteId = siteId;
            LayerId = layerId;
            MarkerId = markerId;
        }
    }
}
