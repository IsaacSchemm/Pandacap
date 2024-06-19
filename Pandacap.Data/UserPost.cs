namespace Pandacap.Data
{
    public class UserPost : IPost, IPostImage
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
        /// Whether this post is considered an artwork post and included in the Gallery section.
        /// </summary>
        public bool Artwork { get; set; }

        public class BlobReference
        {
            public Guid Id { get; set; }
            public string ContentType { get; set; } = "application/octet-stream";

            public string BlobName => $"{Id}";
        };

        /// <summary>
        /// The attached image, if any.
        /// If there is an image, it will be stored in an Azure Storage account, and proxied through ImagesController.
        /// </summary>
        public BlobReference? Image { get; set; }

        /// <summary>
        /// A thumbnail for the attached image, if any.
        /// </summary>
        public BlobReference? Thumbnail { get; set; }

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

        string IPost.Id => $"{Id}";

        string IPost.DisplayTitle => Title ?? $"{Id}";

        DateTimeOffset IPost.Timestamp => PublishedTime;

        string IPost.LinkUrl => $"/UserPosts/{Id}";

        string? IPost.Username => null;

        string? IPost.Usericon => null;

        IEnumerable<IPostImage> IPost.Images => Image != null ? [this] : [];

        string? IPostImage.ThumbnailUrl => $"/Blobs/Thumbnails/{Id}";
    }
}
