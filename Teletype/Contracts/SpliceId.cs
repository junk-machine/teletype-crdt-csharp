namespace Teletype.Contracts
{
    /// <summary>
    /// Identifier of the text splice.
    /// </summary>
    public struct SpliceId
    {
        /// <summary>
        /// Identifier of site where this splice originates.
        /// </summary>
        public readonly int SiteId;

        /// <summary>
        /// Sequence number for the splice within the site.
        /// </summary>
        public readonly int SequenceNumber;

        /// <summary>
        /// Initializes a <see cref="SpliceId"/> structure
        /// with the given site identifier and sequence number.
        /// </summary>
        /// <param name="siteId">Identifier of the site</param>
        /// <param name="sequenceNumber">Sequence number</param>
        public SpliceId(int siteId, int sequenceNumber)
        {
            SiteId = siteId;
            SequenceNumber = sequenceNumber;
        }

        /// <summary>
        /// Checks if two <see cref="SpliceId"/> values are equal.
        /// </summary>
        /// <param name="left">First value</param>
        /// <param name="right">Second value</param>
        /// <returns>true if values are equal, otherwise false.</returns>
        public static bool operator ==(SpliceId left, SpliceId right)
        {
            return left.SiteId == right.SiteId
                && left.SequenceNumber == right.SequenceNumber;
        }

        /// <summary>
        /// Checks if two <see cref="SpliceId"/> values are not equal.
        /// </summary>
        /// <param name="left">First value</param>
        /// <param name="right">Second value</param>
        /// <returns>true if values are not equal, otherwise false.</returns>
        public static bool operator !=(SpliceId left, SpliceId right)
        {
            return left.SiteId != right.SiteId
                && left.SequenceNumber != right.SequenceNumber;
        }

        /// <summary>
        /// Computes the hash for current splice identifier.
        /// </summary>
        /// <returns>Hash value for the current splice identifier.</returns>
        public override int GetHashCode()
        {
            return (SiteId << 22) ^ SequenceNumber;
        }

        /// <summary>
        /// Compares current splice identifier with another object.
        /// </summary>
        /// <param name="obj">Another object to compare with this splice identifier</param>
        /// <returns>true if objects are the same, otheriwise false.</returns>
        public override bool Equals(object obj)
        {
            if (obj is SpliceId)
            {
                return this == (SpliceId)obj;
            }

            return false;
        }

        /// <summary>
        /// Formats site identifier and sequence number for the splice as a string.
        /// </summary>
        /// <returns>Site identifier and sequence number for the splice.</returns>
        public override string ToString()
        {
            return $"{SiteId}-{SequenceNumber}";
        }
    }
}
