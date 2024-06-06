﻿namespace Pandacap.Data
{
    public class UserArtworkDeviation : IUserDeviation, IPost, IThumbnail
    {
        public Guid Id { get; set; }
        public string? LinkUrl { get; set; }
        public string? Title { get; set; }
        public string? Username { get; set; }
        public string? Usericon { get; set; }
        public DateTimeOffset PublishedTime { get; set; }
        public bool IsMature { get; set; }

        public string? Description { get; set; }
        public List<string> Tags { get; set; } = [];

        public string ImageUrl { get; set; } = "";
        public string ImageContentType { get; set; } = "";

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

        DateTimeOffset? IPost.DismissedAt => DateTimeOffset.MinValue;

        IEnumerable<IThumbnail> IPost.Thumbnails => ThumbnailRenditions.Count > 0 ? [this] : [];

        IEnumerable<IThumbnailRendition> IThumbnail.Renditions => ThumbnailRenditions;

        IEnumerable<string> IUserDeviation.Tags => Tags;
    }
}
