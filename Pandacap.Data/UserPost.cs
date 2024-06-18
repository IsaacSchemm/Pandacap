﻿namespace Pandacap.Data
{
    public class UserPost : IPost, IThumbnail
    {
        /// <summary>
        /// A unique ID for this post.
        /// For posts imported from DeviantArt, this should match the ID in the DeviantArt API.
        /// </summary>
        public Guid Id { get; set; }

        /// <summary>
        /// The title of the post, if any.
        /// </summary>
        public string? Title { get; set; }

        /// <summary>
        /// Whether this post has an image attached.
        /// </summary>
        public bool HasImage { get; set; }

        /// <summary>
        /// The expected media type of the image (such as image/png).
        /// </summary>
        public string? ImageContentType { get; set; }

        /// <summary>
        /// Descriptive text for the contents of the image, if any.
        /// </summary>
        public string? AltText { get; set; }

        /// <summary>
        /// Whether this post contains mature content.
        /// </summary>
        public bool IsMature { get; set; }

        /// <summary>
        /// The HTML description of the post, if any.
        /// </summary>
        public string? Description { get; set; }

        /// <summary>
        /// Tags attached to the post, if any.
        /// </summary>
        public List<string> Tags { get; set; } = [];

        /// <summary>
        /// The date and time at which this post was created on DeviantArt.
        /// </summary>
        public DateTimeOffset PublishedTime { get; set; }

        /// <summary>
        /// Whether to hide the title of this post when displaying the full contents.
        /// </summary>
        public bool HideTitle { get; set; }

        /// <summary>
        /// Whether this post should be rendered in ActivityStreams as an Article (instead of a Note).
        /// </summary>
        public bool IsArticle { get; set; }

        public class DeviantArtThumbnailRendition : IThumbnailRendition
        {
            public string Url { get; set; } = "";
            public int Width { get; set; }
            public int Height { get; set; }
        }

        /// <summary>
        /// A list of thumbnail renditions, of different qualities, for this post.
        /// </summary>
        public List<DeviantArtThumbnailRendition> ThumbnailRenditions { get; set; } = [];

        string IPost.Id => $"{Id}";

        string IPost.DisplayTitle => Title ?? $"{Id}";

        DateTimeOffset IPost.Timestamp => PublishedTime;

        string IPost.LinkUrl => $"/UserPost/{Id}";

        IEnumerable<IThumbnail> IPost.Thumbnails => ThumbnailRenditions.Count > 0 ? [this] : [];

        IEnumerable<IThumbnailRendition> IThumbnail.Renditions => ThumbnailRenditions;

        string? IPost.Username => null;

        string? IPost.Usericon => null;
    }
}
