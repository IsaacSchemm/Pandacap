namespace Pandacap.Data
{
    public abstract class InboxDeviation : IPost
    {
        public Guid Id { get; set; }

        public Guid CreatedBy { get; set; }

        public string? Username { get; set; }
        public string? Usericon { get; set; }

        public DateTimeOffset Timestamp { get; set; }
        public bool MatureContent { get; set; }

        public string? Title { get; set; }
        public string? LinkUrl { get; set; }

        public virtual IEnumerable<IThumbnail> Thumbnails => [];

        public DateTimeOffset? DismissedAt { get; set; }

        string IPost.Id => $"{Id}";

        string? IPost.DisplayTitle
        {
            get
            {
                string? excerpt = (this as InboxTextDeviation)?.Excerpt;
                if (excerpt != null && excerpt.Length > 40)
                    excerpt = excerpt[..40] + "...";

                return Title ?? excerpt ?? $"{Id}";
            }
        }
    }
}
