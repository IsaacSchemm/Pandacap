﻿using Pandacap.Data;
using Pandacap.LowLevel;
using System.ServiceModel.Syndication;
using System.Text;
using System.Xml;

namespace Pandacap.HighLevel
{
    /// <summary>
    /// Builds Atom and RSS feeds for the outbox.
    /// </summary>
    public class FeedBuilder(IdMapper mapper, ApplicationInformation appInfo)
    {
        /// <summary>
        /// Generates an HTML rendition of the post, including image(s), description, and outgoing link(s).
        /// </summary>
        /// <param name="post">The submission to render</param>
        /// <returns>A sequence of HTML strings that should be concatenated</returns>
        private IEnumerable<string> GetHtml(UserPost post)
        {
            if (!post.IsMature)
            {
                if (!post.ImageBlobs.IsEmpty)
                    yield return $"<p><img src='{mapper.GetImageUrl(post, post.ImageBlobs[0])}' height='250' /></p>";
                if (post.Description != null)
                    yield return post.Description;
            }
        }

        /// <summary>
        /// Creates a feed item for a post.
        /// </summary>
        /// <param name="post">The submission to render</param>
        /// <returns>A feed item</returns>
        private SyndicationItem ToSyndicationItem(UserPost post)
        {
            var item = new SyndicationItem
            {
                Id = mapper.GetObjectId(post),
                PublishDate = post.PublishedTime,
                LastUpdatedTime = post.PublishedTime,
                Content = new TextSyndicationContent(string.Join(" ", GetHtml(post)), TextSyndicationContentKind.Html)
            };

            if (!post.HideTitle)
                item.Title = new TextSyndicationContent(post.Title, TextSyndicationContentKind.Plaintext);

            item.Links.Add(SyndicationLink.CreateAlternateLink(new Uri(mapper.GetObjectId(post)), "text/html"));

            return item;
        }

        /// <summary>
        /// Creates a feed for a list of posts.
        /// </summary>
        /// <param name="person">The author of the posts</param>
        /// <param name="posts">A sequence of submissions</param>
        /// <returns>A feed object</returns>
        private SyndicationFeed ToSyndicationFeed(IEnumerable<UserPost> posts)
        {
            string uri = $"{mapper.ActorId}/feed";
            var feed = new SyndicationFeed
            {
                Id = uri,
                Title = new TextSyndicationContent($"@{appInfo.Username}@{appInfo.HandleHostname}", TextSyndicationContentKind.Plaintext),
                Description = new TextSyndicationContent($"Pandacap posts from {appInfo.Username}", TextSyndicationContentKind.Plaintext),
                Copyright = new TextSyndicationContent($"{appInfo.Username}", TextSyndicationContentKind.Plaintext),
                LastUpdatedTime = posts.Select(x => x.PublishedTime).Max(),
                ImageUrl = new Uri(mapper.AvatarUrl),
                Items = posts.Select(ToSyndicationItem)
            };
            feed.Links.Add(SyndicationLink.CreateSelfLink(new Uri(uri), "application/rss+xml"));
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
        /// Generates an RSS feed for a list of posts.
        /// </summary>
        /// <param name="person">The author of the posts</param>
        /// <param name="posts">A sequence of submissions</param>
        /// <returns>An RSS feed (should be serialized as UTF-8)</returns>
        public string ToRssFeed(IEnumerable<UserPost> posts)
        {
            var feed = ToSyndicationFeed(posts);

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
        /// <param name="person">The author of the posts</param>
        /// <param name="posts">A sequence of submissions</param>
        /// <returns>An Atom feed (should be serialized as UTF-8)</returns>
        public string ToAtomFeed(IEnumerable<UserPost> posts)
        {
            var feed = ToSyndicationFeed(posts);

            using var sw = new UTF8StringWriter();

            using (var xmlWriter = XmlWriter.Create(sw))
            {
                new Atom10FeedFormatter(feed).WriteTo(xmlWriter);
            }

            return sw.ToString();
        }
    }
}
