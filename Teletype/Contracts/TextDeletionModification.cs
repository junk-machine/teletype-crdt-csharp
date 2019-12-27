using System.Collections.Generic;

namespace Teletype.Contracts
{
    /// <summary>
    /// Document modification where text is being deleted.
    /// </summary>
    public class TextDeletionModification : TextModificationBase
    {
        /// <summary>
        /// Gets the identifier of the splice that represents this deletion operation.
        /// </summary>
        public SpliceId SpliceId { get; }

        /// <summary>
        /// Gets the latest sequence number for each site required by an operation.
        /// </summary>
        public IReadOnlyDictionary<int, int> MaxSequenceNumberBySite { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="TextDeletionModification"/> class
        /// with the provided splice identifier, max sequences number by site, identifiers of
        /// start and end splices as well as the respective character offsets.
        /// </summary>
        /// <param name="spliceId">Identifier of the splice that represents this deletion operation</param>
        /// <param name="maxSequenceNumberBySite">Latest sequence number for each site</param>
        /// <param name="leftDependencyId">Identifier of the splice where modification starts</param>
        /// <param name="offsetInLeftDependency">Character offset in the starting splice</param>
        /// <param name="rightDependencyId">Identifier of the splice where modification ends</param>
        /// <param name="offsetInRightDependency">Character offset in the ending splice</param>
        public TextDeletionModification(
            SpliceId spliceId,
            IReadOnlyDictionary<int, int> maxSequenceNumberBySite,
            SpliceId leftDependencyId,
            Point offsetInLeftDependency,
            SpliceId rightDependencyId,
            Point offsetInRightDependency)
            : base(leftDependencyId, offsetInLeftDependency, rightDependencyId, offsetInRightDependency)
        {
            SpliceId = spliceId;
            MaxSequenceNumberBySite = maxSequenceNumberBySite;
        }
    }
}
