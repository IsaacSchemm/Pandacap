using Pandacap.UI.Badges;

namespace Pandacap.UI.Elements
{
    /// <summary>
    /// A post that can be shown in one of Pandacap's "paged" areas, like the gallery or inbox.
    /// </summary>
    public interface IPost
    {
        /// <summary>
        /// A badge that shows the origin of a remote post.
        /// </summary>
        Badge Badge { get; }

        /// <summary>
        /// The title to be shown for this post in a paged view.
        /// </summary>
        string DisplayTitle { get; }

        /// <summary>
        /// An opaque ID for this post; used in pagination.
        /// </summary>
        string Id { get; }

        /// <summary>
        /// A URL where the Pandacap administrator can view this content.
        /// </summary>
        string? InternalUrl { get; }

        /// <summary>
        /// A URL where unauthenticated users can view this content.
        /// </summary>
        string? ExternalUrl { get; }

        /// <summary>
        /// The date/time at which this content was posted or added.
        /// </summary>
        DateTimeOffset PostedAt { get; }

        /// <summary>
        /// The URL of the profile page of the user who posted this content, if any.
        /// </summary>
        string? ProfileUrl { get; }

        /// <summary>
        /// A list of thumbnails associated with this content. Can be an empty list.
        /// </summary>
        IEnumerable<IPostThumbnail> Thumbnails { get; }

        /// <summary>
        /// The name of the user who posted this content.
        /// </summary>
        string? Username { get; }

        /// <summary>
        /// An avatar associated with the user who posted this content, if any.
        /// </summary>
        string? Usericon { get; }
    }
}
