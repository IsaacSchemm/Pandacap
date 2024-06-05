namespace Pandacap.Data
{
    public class InboxImageDeviation : IPost, IThumbnail
    {
        public Guid Id { get; set; }

        public Guid CreatedBy { get; set; }

        public string? Username { get; set; }
        public string? Usericon { get; set; }

        public DateTimeOffset Timestamp { get; set; }
        public bool MatureContent { get; set; }

        public string? Title { get; set; }
        public string? LinkUrl { get; set; }

        public class DeviantArtThumbnailRendition : IThumbnailRendition
        {
            public string Url { get; set; } = "";
            public int Width { get; set; }
            public int Height { get; set; }
        }

        public List<DeviantArtThumbnailRendition> ThumbnailRenditions { get; set; } = [];

        public DateTimeOffset? DismissedAt { get; set; }

        string IPost.Id => $"{Id}";

        string? IPost.DisplayTitle => Title ?? $"{Id}";

        IEnumerable<IThumbnail> IPost.Thumbnails => ThumbnailRenditions.Count > 0 ? [this] : [];

        string? IThumbnail.AltText => null;

        IEnumerable<IThumbnailRendition> IThumbnail.Renditions => ThumbnailRenditions;
    }
}
