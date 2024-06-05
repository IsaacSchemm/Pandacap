namespace Pandacap.Data
{
    public class DeviantArtArtworkDeviation : IDeviation, IPost, IThumbnail
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

        public class DeviantArtImage : IDeviationImage
        {
            public string Url { get; set; } = "";
            public string ContentType { get; set; } = "";
            public int Width { get; set; }
            public int Height { get; set; }
        }

        public DeviantArtImage Image { get; set; } = new();

        public class DeviantArtThumbnailRendition : IThumbnailRendition
        {
            public string Url { get; set; } = "";
            public int Width { get; set; }
            public int Height { get; set; }
        }

        public List<DeviantArtThumbnailRendition> ThumbnailRenditions { get; set; } = [];

        public string? AltText { get; set; }

        string IPost.Id => $"{Id}";

        string? IPost.DisplayTitle => Title ?? $"{Id}";

        DateTimeOffset IPost.Timestamp => PublishedTime;

        string? IPost.LinkUrl => Url;

        DateTimeOffset? IPost.DismissedAt => null;

        IEnumerable<IThumbnail> IPost.Thumbnails => [this];

        IEnumerable<IThumbnailRendition> IThumbnail.Renditions => ThumbnailRenditions;

        IDeviationImage? IDeviation.Image => Image;

        IEnumerable<string> IDeviation.Tags => Tags;
    }
}
