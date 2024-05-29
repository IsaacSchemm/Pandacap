namespace Pandacap.Data
{
    public abstract class DeviantArtOurPost
    {
        public Guid Id { get; set; }
        public string? Url { get; set; }
        public string? Title { get; set; }
        public string? Username { get; set; }
        public string? Usericon { get; set; }
        public DateTimeOffset PublishedTime { get; set; }
        public bool IsMature { get; set; }

        public string? Description { get; set; }
        public List<string> Tags { get; set; } = [];

        /// <summary>
        /// The last time Pandacap attempted to refresh this item.
        /// </summary>
        public DateTimeOffset CacheRefreshAttemptedAt { get; set; }

        /// <summary>
        /// The last time Pandacap successfully refreshed this item.
        /// </summary>
        public DateTimeOffset CacheRefreshSucceededAt { get; set; }
    }
}
