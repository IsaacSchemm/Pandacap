using Pandacap.Configuration;
using Pandacap.Constants;
using Pandacap.Database;
using Pandacap.Database.Extensions;
using Pandacap.FeedCreation.Outbox.Interfaces;
using System.ServiceModel.Syndication;
using System.Text;
using System.Xml;

namespace Pandacap.FeedCreation.Outbox
{
    /// <summary>
    /// Builds Atom and RSS feeds for the outbox.
    /// </summary>
    internal class FeedBuilder : IFeedBuilder
    {
        /// <summary>
        /// Generates an HTML rendition of the post, including image(s), description, and outgoing link(s).
        /// </summary>
        /// <param name="post">The submission to render</param>
        /// <returns>A sequence of HTML strings that should be concatenated</returns>
        private static IEnumerable<string> GetHtml(Post post)
        {
            if (post.Html is string html)
                yield return html;

            yield return post.GenerateLinksHtml();

            foreach (var image in post.Images)
                yield return $"<p><img src='{post.GetImageUrl(image)}' height='250' style='max-height: 250px' /></p>";
        }

        /// <summary>
        /// Creates a feed item for a post.
        /// </summary>
        /// <param name="post">The submission to render</param>
        /// <returns>A feed item</returns>
        private SyndicationItem ToSyndicationItem(Post post)
        {
            string url = post.Url;

            var item = new SyndicationItem
            {
                Id = url,
                PublishDate = post.PublishedTime,
                LastUpdatedTime = post.PublishedTime,
                Content = new TextSyndicationContent(string.Join(" ", GetHtml(post)), TextSyndicationContentKind.Html)
            };

            if (post.Title != null)
                item.Title = new TextSyndicationContent(post.Title, TextSyndicationContentKind.Plaintext);

            item.Links.Add(SyndicationLink.CreateAlternateLink(new Uri(url), "text/html"));

            return item;
        }

        /// <summary>
        /// Creates a feed for a list of posts.
        /// </summary>
        /// <param name="posts">A sequence of submissions</param>
        /// <param name="url">The feed's URL</param>
        /// <returns>A feed object</returns>
        private SyndicationFeed ToSyndicationFeed(IEnumerable<Post> posts, string url)
        {
            var feed = new SyndicationFeed
            {
                Id = url,
                Title = new TextSyndicationContent($"{DeploymentInformation.Username} ({UserAgentInformation.ApplicationName})", TextSyndicationContentKind.Plaintext),
                Description = new TextSyndicationContent($"{UserAgentInformation.ApplicationName} posts from {DeploymentInformation.Username}", TextSyndicationContentKind.Plaintext),
                Copyright = new TextSyndicationContent($"{DeploymentInformation.Username}", TextSyndicationContentKind.Plaintext),
                LastUpdatedTime = posts.Max(x => x.PublishedTime),
                Items = posts.Select(ToSyndicationItem)
            };
            feed.Links.Add(SyndicationLink.CreateSelfLink(new Uri(url), "application/rss+xml"));
            feed.Links.Add(SyndicationLink.CreateAlternateLink(new Uri($"https://{DeploymentInformation.ApplicationHostname}"), "text/html"));
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
        /// Generates an RSS feed for a list of posts.
        /// </summary>
        /// <param name="url">The feed's URL</param>
        /// <param name="posts">A sequence of submissions</param>
        /// <returns>An RSS feed (should be serialized as UTF-8)</returns>
        public string ToRssFeed(string url, IEnumerable<Post> posts)
        {
            var feed = ToSyndicationFeed(posts, url);

            using var sw = new UTF8StringWriter();

            using (var xmlWriter = XmlWriter.Create(sw))
            {
                new Rss20FeedFormatter(feed).WriteTo(xmlWriter);
            }

            return sw.ToString();
        }

        /// <summary>
        /// Generates an Atom feed for a list of posts.
        /// </summary>
        /// <param name="url">The feed's URL</param>
        /// <param name="posts">A sequence of submissions</param>
        /// <returns>An Atom feed (should be serialized as UTF-8)</returns>
        public string ToAtomFeed(string url, IEnumerable<Post> posts)
        {
            var feed = ToSyndicationFeed(posts, url);

            using var sw = new UTF8StringWriter();

            using (var xmlWriter = XmlWriter.Create(sw))
            {
                new Atom10FeedFormatter(feed).WriteTo(xmlWriter);
            }

            return sw.ToString();
        }
    }
}
