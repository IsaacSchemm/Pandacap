using Pandacap.UI.Badges;

namespace Pandacap.UI.Elements
{
    /// <summary>
    /// A user or feed which the Pandacap admin is following, as shown in the UI.
    /// </summary>
    public interface IFollow
    {
        /// <summary>
        /// A badge that shows the origin of a remote user or feed.
        /// </summary>
        Badge Badge { get; }

        /// <summary>
        /// A URL where unauthenticated users can view this user or feed, if any.
        /// </summary>
        string? LinkUrl { get; }

        /// <summary>
        /// A name for this user or feed.
        /// </summary>
        string Username { get; }

        /// <summary>
        /// An avatar associated with this user or feed, if any.
        /// </summary>
        string? IconUrl { get; }

        /// <summary>
        /// Whether the content from this user that shows up in Pandacap's inbox is filtered in some way (for example, by hiding reposts).
        /// </summary>
        bool Filtered { get; }
    }
}
