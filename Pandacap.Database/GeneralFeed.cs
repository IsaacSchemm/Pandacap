using Pandacap.UI.Badges;
using Pandacap.UI.Elements;

namespace Pandacap.Database
{
    /// <summary>
    /// A syndication feed (RSS, Atom, or another supported format) that the Pandacap admin is following.
    /// </summary>
    public class GeneralFeed : IFollow
    {
        /// <summary>
        /// A local Pandacap ID for the feed.
        /// </summary>
        public Guid Id { get; set; }

        /// <summary>
        /// The feed URL.
        /// </summary>
        public string FeedUrl { get; set; } = "";

        /// <summary>
        /// The title of the feed, if any.
        /// </summary>
        public string? FeedTitle { get; set; }

        /// <summary>
        /// The URL of a website associated with the feed, if any.
        /// </summary>
        public string? FeedWebsiteUrl { get; set; }

        /// <summary>
        /// The URL of an avatar for the feed, if any.
        /// </summary>
        public string? FeedIconUrl { get; set; }

        /// <summary>
        /// When Pandacap last checked the feed for new posts.
        /// </summary>
        public DateTimeOffset LastCheckedAt { get; set; } = DateTimeOffset.MinValue;

        Badge IFollow.Badge => Badges.Feeds;

        string? IFollow.LinkUrl => FeedWebsiteUrl;

        string IFollow.Username => FeedTitle ?? FeedUrl;

        string? IFollow.IconUrl => FeedIconUrl;

        bool IFollow.Filtered => false;
    }
}
