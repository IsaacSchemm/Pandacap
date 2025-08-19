using Pandacap.ConfigurationObjects;
using Pandacap.Data;
using System.Net;
using System.ServiceModel.Syndication;
using System.Text;
using System.Xml;

namespace Pandacap.HighLevel.RssOutbound
{
    /// <summary>
    /// Builds Atom and RSS feeds for the outbox.
    /// </summary>
    public class FavoritesFeedBuilder(ApplicationInformation appInfo)
    {
        /// <summary>
        /// Generates an HTML rendition of the favorite.
        /// </summary>
        /// <param name="post">The favorite to render</param>
        /// <returns>A sequence of HTML strings that should be concatenated</returns>
        private static IEnumerable<string> GetHtml(IFavorite favorite)
        {
            if (favorite.DisplayTitle is string title)
                yield return $"<h1>{WebUtility.HtmlEncode(title)}</h1>";

            string linkify(string html, string? url) => url is string u
                ? $"<a href='{u}'>{html}</p>"
                : html;

            if (favorite.Username is string username)
                yield return $"<p>by {linkify(WebUtility.HtmlEncode(favorite.Username), favorite.ProfileUrl)}</p>";

            foreach (var thumbnail in favorite.Thumbnails)
                yield return $"<p>{linkify($"<img src='{thumbnail.Url}' height='250' alt='{thumbnail.AltText}' />", favorite.ExternalUrl)}</p>";

            string platformName = favorite.Platform.ToString();

            if (favorite.ExternalUrl is string url)
                yield return $"<p><a href='{url}'>View on {WebUtility.HtmlEncode(platformName)}</a></p>";
        }

        /// <summary>
        /// Creates a feed item for a favorite.
        /// </summary>
        /// <param name="favorite">The favorite to render</param>
        /// <returns>A feed item</returns>
        private SyndicationItem ToSyndicationItem(IFavorite favorite)
        {
            string url = $"https://{appInfo.ApplicationHostname}/Starpass/{favorite.Id}";

            var item = new SyndicationItem
            {
                Id = url,
                PublishDate = favorite.PostedAt,
                LastUpdatedTime = favorite.PostedAt,
                Content = new TextSyndicationContent(string.Join(" ", GetHtml(favorite)), TextSyndicationContentKind.Html)
            };

            if (favorite.DisplayTitle is string title)
                item.Title = new TextSyndicationContent(title, TextSyndicationContentKind.Plaintext);

            item.Links.Add(SyndicationLink.CreateAlternateLink(new Uri(url), "text/html"));

            return item;
        }

        /// <summary>
        /// Creates a feed for a list of favorites.
        /// </summary>
        /// <param name="favorites">A sequence of favorites</param>
        /// <param name="url">The feed's URL</param>
        /// <returns>A feed object</returns>
        private SyndicationFeed ToSyndicationFeed(IEnumerable<IFavorite> favorites, string url)
        {
            var feed = new SyndicationFeed
            {
                Id = url,
                Title = new TextSyndicationContent($"Starpass ({appInfo.Username})", TextSyndicationContentKind.Plaintext),
                Description = new TextSyndicationContent($"An automated feed of {appInfo.Username}'s favorites and likes (past 30 days), mirrored from {appInfo.ApplicationHostname}", TextSyndicationContentKind.Plaintext),
                Copyright = new TextSyndicationContent("Respective post authors", TextSyndicationContentKind.Plaintext),
                LastUpdatedTime = favorites.Select(x => x.FavoritedAt).Max(),
                Items = favorites.Select(ToSyndicationItem)
            };
            feed.Links.Add(SyndicationLink.CreateSelfLink(new Uri(url), "application/rss+xml"));
            feed.Links.Add(SyndicationLink.CreateAlternateLink(new Uri($"https://{appInfo.ApplicationHostname}"), "text/html"));
            return feed;
        }

        /// <summary>
        /// A StringWriter that tells the XmlWriter to declare the encoding as UTF-8.
        /// </summary>
        private class UTF8StringWriter : StringWriter
        {
            public override Encoding Encoding => Encoding.UTF8;
        }

        /// <summary>
        /// Generates an RSS feed for a list of favorites.
        /// </summary>
        /// <param name="favorites">A sequence of favorites</param>
        /// <param name="url">The feed's URL</param>
        /// <returns>An RSS feed (should be serialized as UTF-8)</returns>
        public string ToRssFeed(IEnumerable<IFavorite> favorites, string url)
        {
            var feed = ToSyndicationFeed(favorites, url);

            using var sw = new UTF8StringWriter();

            using (var xmlWriter = XmlWriter.Create(sw))
            {
                new Rss20FeedFormatter(feed).WriteTo(xmlWriter);
            }

            return sw.ToString();
        }

        /// <summary>
        /// Generates an Atom feed for a list of favorites.
        /// </summary>
        /// <param name="favorites">A sequence of submissions</param>
        /// <param name="url">The feed's URL</param>
        /// <returns>An Atom feed (should be serialized as UTF-8)</returns>
        public string ToAtomFeed(IEnumerable<IFavorite> favorites, string url)
        {
            var feed = ToSyndicationFeed(favorites, url);

            using var sw = new UTF8StringWriter();

            using (var xmlWriter = XmlWriter.Create(sw))
            {
                new Atom10FeedFormatter(feed).WriteTo(xmlWriter);
            }

            return sw.ToString();
        }
    }
}
