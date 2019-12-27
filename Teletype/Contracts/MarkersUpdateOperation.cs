using System.Collections.Generic;

namespace Teletype.Contracts
{
    /// <summary>
    /// Defines markers changes for a given site.
    /// </summary>
    /// <remarks>
    /// Equivalent of <code>{ 'type': 'markers-update' }</code> in the original implementation.
    /// </remarks>
    public class MarkersUpdateOperation : IOperation
    {
        /// <summary>
        /// Gets the identifier of the site.
        /// </summary>
        public int SiteId { get; }

        /// <summary>
        /// Gets marker updates by layer ID and marker ID.
        /// </summary>
        public IReadOnlyDictionary<int, IReadOnlyDictionary<int, Marker<LogicalRange>>> Updates { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="MarkerOperation"/> class
        /// with the provided site identifier and marker updates.
        /// </summary>
        /// <param name="siteId">Identifier of the site</param>
        /// <param name="updates">Markers for the site</param>
        public MarkersUpdateOperation(int siteId, IReadOnlyDictionary<int, IReadOnlyDictionary<int, Marker<LogicalRange>>> updates)
        {
            SiteId = siteId;
            Updates = updates;
        }
    }
}
