namespace Pandacap.Data
{
    public class InboxArtworkDeviation : IPost, IPostImage
    {
        public Guid Id { get; set; }

        public Guid CreatedBy { get; set; }

        public string? Username { get; set; }
        public string? Usericon { get; set; }

        public DateTimeOffset Timestamp { get; set; }
        public bool MatureContent { get; set; }

        public string? Title { get; set; }
        public string? LinkUrl { get; set; }

        public class DeviantArtThumbnailRendition
        {
            public string Url { get; set; } = "";
            public int Width { get; set; }
            public int Height { get; set; }
        }

        public string? ThumbnailUrl { get; set; }

        public DateTimeOffset? DismissedAt { get; set; }

        string IPost.Id => $"{Id}";

        string IPost.DisplayTitle => Title ?? $"{Id}";

        IEnumerable<IPostImage> IPost.Images => [this];

        string? IPostImage.AltText => null;
    }
}
