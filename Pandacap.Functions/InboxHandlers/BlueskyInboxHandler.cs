using Microsoft.EntityFrameworkCore;
using Pandacap.Clients.ATProto;
using Pandacap.ConfigurationObjects;
using Pandacap.Data;
using Pandacap.HighLevel.ATProto;
using BlueskyFeed = Pandacap.Clients.ATProto.BlueskyFeed;

namespace Pandacap.Functions.InboxHandlers
{
    public class BlueskyInboxHandler(
        PandacapDbContext context,
        ATProtoDIDResolver didResolver,
        IHttpClientFactory httpClientFactory)
    {
        private const string BlueskyModerationService = "did:plc:ar7c4by46qjdydhdevvrndac";

        private static readonly IReadOnlyList<string> BlueskyModerationServiceAdultContentLabels = [
            "porn",
            "sexual",
            "nudity",
            "sexual-figurative",
            "graphic-media"
        ];

        private static async IAsyncEnumerable<BlueskyFeed.FeedItem> WrapAsync(
            Func<Page, Task<BlueskyFeed.FeedResponse>> handler)
        {
            var page = Page.FromStart;

            while (true)
            {
                var results = await handler(page);

                foreach (var item in results.feed)
                    yield return item;

                if (results.NextPage.IsEmpty)
                    break;

                page = results.NextPage.Single();
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

                List<BlueskyFeedItem> newItems = [];

                feed.LastRefreshedAt = DateTimeOffset.UtcNow;

                await foreach (var feedItem in WrapAsync(page => BlueskyFeed.GetAuthorFeedAsync(client, feed.PDS, feed.DID, page)))
                {
                    if (feedItem.IndexedAt <= feed.LastPostedAt)
                        break;

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

                    feed.Avatar = feedItem.post.author.AvatarOrNull;
                    feed.DisplayName = feedItem.post.author.DisplayNameOrNull;
                    feed.Handle = feedItem.post.author.handle;

                    newItems.Add(new()
                    {
                        Id = Guid.NewGuid(),
                        CID = feedItem.post.cid,
                        RecordKey = feedItem.post.RecordKey,
                        Author = new()
                        {
                            DID = feedItem.post.author.did,
                            PDS = await didResolver.GetPDSAsync(feedItem.post.author.did),
                            DisplayName = feedItem.post.author.DisplayNameOrNull,
                            Handle = feedItem.post.author.handle,
                            Avatar = feedItem.post.author.AvatarOrNull
                        },
                        PostedBy = new()
                        {
                            DID = feedItem.By.did,
                            PDS = await didResolver.GetPDSAsync(feedItem.By.did),
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
                    });

                    if (newItems.Count >= 20)
                        break;
                }

                context.BlueskyFeedItems.AddRange(newItems);

                feed.LastPostedAt = newItems
                    .Select(f => f.IndexedAt)
                    .Concat([feed.LastPostedAt])
                    .Max();

                await context.SaveChangesAsync();
            }
        }
    }
}
