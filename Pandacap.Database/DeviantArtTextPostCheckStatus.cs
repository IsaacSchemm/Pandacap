namespace Pandacap.Database
{
    /// <summary>
    /// An object to keep track of the last time posts were imported to the inbox.
    /// </summary>
    public class DeviantArtTextPostCheckStatus
    {
        public Guid Id { get; set; }

        /// <summary>
        /// The date/time at which Pandacap last checked for DeviantArt text posts
        /// from followed users. Any users who haven't visited DeviantArt since
        /// this time will be skipped.
        /// </summary>
        public DateTimeOffset LastCheck { get; set; }
    }
}
