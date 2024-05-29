namespace Pandacap.Data
{
    public abstract class DeviantArtOurPost : IPost
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

        string IPost.Id => $"{Id}";

        string? IPost.DisplayTitle
        {
            get
            {
                string? excerpt = (this as DeviantArtOurTextPost)?.Excerpt;
                if (excerpt != null && excerpt.Length > 40)
                    excerpt = excerpt[..40] + "...";

                return Title ?? excerpt ?? $"{Id}";
            }
        }

        DateTimeOffset IPost.Timestamp => PublishedTime;

        string? IPost.LinkUrl => Url;

        DateTimeOffset? IPost.DismissedAt => null;
    }
}
