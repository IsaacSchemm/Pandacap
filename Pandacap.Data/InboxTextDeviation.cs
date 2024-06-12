namespace Pandacap.Data
{
    public class InboxTextDeviation : IPost
    {
        public Guid Id { get; set; }

        public Guid CreatedBy { get; set; }

        public string? Username { get; set; }
        public string? Usericon { get; set; }

        public DateTimeOffset Timestamp { get; set; }
        public bool MatureContent { get; set; }

        public string? Title { get; set; }
        public string? LinkUrl { get; set; }

        public string? Excerpt { get; set; }

        public DateTimeOffset? DismissedAt { get; set; }

        string IPost.Id => $"{Id}";

        string IPost.DisplayTitle => Title ?? $"{Id}";

        IEnumerable<IThumbnail> IPost.Thumbnails => [];
    }
}
