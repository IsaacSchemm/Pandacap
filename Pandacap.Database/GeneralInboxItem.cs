using Pandacap.Text;
using Pandacap.UI.Badges;
using Pandacap.UI.Elements;
using System.ComponentModel.DataAnnotations.Schema;

namespace Pandacap.Database
{
    /// <summary>
    /// A generic inbox item corresponding to an RSS, Atom, or other type of feed.
    /// </summary>
    public class GeneralInboxItem : IInboxPost
    {
        public Guid Id { get; set; }

        public string? FeedTitle { get; set; }
        public string FeedWebsiteUrl { get; set; } = "";
        public string? FeedIconUrl { get; set; }

        public string? Title { get; set; }
        public string? HtmlBody { get; set; }
        public string? TextBody { get; set; }
        public string Url { get; set; } = "";

        public DateTimeOffset Timestamp { get; set; }

        public string? ThumbnailUrl { get; set; }
        public string? ThumbnailAltText { get; set; }

        public string? AudioUrl { get; set; }

        public DateTimeOffset? DismissedAt { get; set; }

        [NotMapped]
        public string DisplayFeedTitle => FeedTitle ?? FeedWebsiteUrl;

        [NotMapped]
        public string DisplayTitle => Title ?? ExcerptGenerator.FromText(60, TextBody ?? HtmlBody ?? "");

        bool IInboxPost.IsPodcast => AudioUrl != null;

        bool IInboxPost.IsShare => false;

        Badge IPost.Badge => Badges.Feeds.WithHostFromUriString(Url ?? FeedWebsiteUrl);

        string IPost.Id => $"{Id}";

        string IPost.InternalUrl => $"/GeneralPosts?id={Id}";

        string IPost.ExternalUrl => Url ?? FeedWebsiteUrl;

        DateTimeOffset IPost.PostedAt => Timestamp;

        string? IPost.ProfileUrl => FeedWebsiteUrl;

        IEnumerable<IPostThumbnail> IPost.Thumbnails
        {
            get
            {
                if (ThumbnailUrl is string url && ThumbnailAltText is string altText)
                    yield return new PostThumbnail(url, altText);
            }
        }

        string? IPost.Username => DisplayFeedTitle;

        string? IPost.Usericon => FeedIconUrl;

        private record PostThumbnail(string Url, string AltText) : IPostThumbnail;
    }
}
