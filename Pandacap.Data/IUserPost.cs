namespace Pandacap.Data
{
    public interface IUserPost
    {
        /// <summary>
        /// A unique ID for the post.
        /// For posts imported from DeviantArt, this should match the ID in the DeviantArt API.
        /// </summary>
        Guid Id { get; }

        /// <summary>
        /// The title of the post, if any.
        /// </summary>
        string? Title { get; }

        /// <summary>
        /// The attached image, if any.
        /// </summary>
        IUserPostImage? Image { get; }

        /// <summary>
        /// The HTML description of the post, if any.
        /// </summary>
        string? Description { get; }

        /// <summary>
        /// Whether this post contains mature content.
        /// </summary>
        bool IsMature { get; }

        /// <summary>
        /// Tags attached to the post, if any.
        /// </summary>
        IEnumerable<string> Tags { get; }

        /// <summary>
        /// The timestamp to display for the post.
        /// </summary>
        DateTimeOffset Timestamp { get; }

        /// <summary>
        /// Whether to hide the title of this post when displaying the full contents.
        /// </summary>
        bool HideTitle { get; }

        /// <summary>
        /// Whether this post should be rendered in ActivityStreams as an Article (instead of a Note).
        /// </summary>
        bool IsArticle { get; }
    }
}
