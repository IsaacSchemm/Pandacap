using CodeHollow.FeedReader;
using Microsoft.EntityFrameworkCore;
using Pandacap.Clients.ATProto;
using Pandacap.ConfigurationObjects;
using Pandacap.Data;
using BlueskyFeed = Pandacap.Clients.ATProto.BlueskyFeed;

namespace Pandacap.Functions.InboxHandlers
{
    public class BlueskyInboxHandler(
        PandacapDbContext context,
        IHttpClientFactory httpClientFactory)
    {
        private const int MAX_POSTS_PER_USER_PER_RUN = 50;

        private const string BlueskyModerationService = "did:plc:ar7c4by46qjdydhdevvrndac";

        private static readonly IReadOnlyList<string> BlueskyModerationServiceAdultContentLabels = [
            "porn",
            "sexual",
            "nudity",
            "sexual-figurative",
            "graphic-media"
        ];

        private static async IAsyncEnumerable<BlueskyFeed.FeedItem> GetAuthorFeedAsync(
            HttpClient client,
            string pds,
            string did)
        {
            var page = Page.FromStart;

            while (true)
            {
                var results = await BlueskyFeed.GetAuthorFeedAsync(client, pds, did, page);

                foreach (var item in results.feed)
                    yield return item;

                if (results.NextPage.IsEmpty)
                    break;

                page = results.NextPage.Single();
            }
        }

        private static async IAsyncEnumerable<BlueskyFeedItem> CollectInboxItems(
            HttpClient client,
            Data.BlueskyFeed feed)
        {
            await foreach (var feedItem in GetAuthorFeedAsync(client, feed.PDS, feed.DID))
            {
                if (feed.MostRecentCIDs != null
                    && feed.MostRecentCIDs.Count > 0
                    && feed.MostRecentCIDs.Contains(feedItem.post.cid))
                {
                    break;
                }

                bool isQuotePost = !feedItem.post.EmbeddedRecords.IsEmpty;
                if (isQuotePost && !feed.IncludeQuotePosts)
                    continue;

                bool isReply = !feedItem.post.record.InReplyTo.IsEmpty;
                if (isReply)
                    continue;

                bool isRepost = feedItem.post.author != feedItem.By;
                if (isRepost)
                {
                    bool hasImages = !feedItem.post.Images.IsEmpty;

                    bool include = hasImages
                        ? feed.IncludeImageShares
                        : feed.IncludeTextShares;

                    if (!include)
                        continue;
                }

                yield return new()
                {
                    Id = Guid.NewGuid(),
                    CID = feedItem.post.cid,
                    RecordKey = feedItem.post.RecordKey,
                    Author = new()
                    {
                        DID = feedItem.post.author.did,
                        PDS = feed.PDS,
                        DisplayName = feedItem.post.author.DisplayNameOrNull,
                        Handle = feedItem.post.author.handle,
                        Avatar = feedItem.post.author.AvatarOrNull
                    },
                    PostedBy = new()
                    {
                        DID = feedItem.By.did,
                        PDS = feed.PDS,
                        DisplayName = feedItem.By.DisplayNameOrNull,
                        Handle = feedItem.By.handle,
                        Avatar = feedItem.By.AvatarOrNull
                    },
                    CreatedAt = feedItem.post.record.createdAt,
                    IndexedAt = feedItem.IndexedAt,
                    IsAdultContent =
                        feedItem.post.labels
                        .Where(l => l.src == feedItem.post.author.did || l.src == BlueskyModerationService)
                        .Select(l => l.val)
                        .Intersect(BlueskyModerationServiceAdultContentLabels)
                        .Any(),
                    Text = feedItem.post.record.text,
                    Images = feed.IgnoreImages
                        ? []
                        : [
                            .. feedItem.post.Images.Select(image => new BlueskyFeedItemImage
                                {
                                    Thumb = image.thumb,
                                    Fullsize = image.fullsize,
                                    Alt = image.alt
                                })
                        ]
                };
            }
        }

        public async Task ReadFeedsAsync()
        {
            using var client = httpClientFactory.CreateClient();
            client.DefaultRequestHeaders.UserAgent.ParseAdd(UserAgentInformation.UserAgent);

            var feeds = await context.BlueskyFeeds
                .OrderBy(feed => feed.LastRefreshedAt)
                .ToListAsync();

            foreach (var feed in feeds)
            {
                if (!feed.ShouldRefresh)
                    continue;

                feed.MostRecentCIDs ??= [];

                int max = feed.MostRecentCIDs.Count == 0
                    ? 5
                    : MAX_POSTS_PER_USER_PER_RUN;

                var newItems = await CollectInboxItems(client, feed)
                    .Take(max)
                    .ToListAsync();

                context.BlueskyFeedItems.AddRange(newItems);

                if (newItems.Count > 0)
                {
                    var firstItem = newItems[0];

                    feed.Avatar = firstItem.PostedBy.Avatar;
                    feed.DisplayName = firstItem.PostedBy.DisplayName;
                    feed.Handle = firstItem.PostedBy.Handle;

                    feed.LastPostedAt = newItems
                        .Select(item => item.IndexedAt)
                        .Max();
                }

                IEnumerable<string> mostRecent = [
                    .. newItems.OrderByDescending(i => i.IndexedAt).Select(i => i.CID),
                    .. feed.MostRecentCIDs
                ];

                feed.MostRecentCIDs = [.. mostRecent.Take(5)];

                feed.LastRefreshedAt = DateTimeOffset.UtcNow;

                await context.SaveChangesAsync();
            }
        }
    }
}
