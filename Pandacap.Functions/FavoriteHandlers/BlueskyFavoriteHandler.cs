using Microsoft.EntityFrameworkCore;
using Pandacap.ConfigurationObjects;
using Pandacap.Data;
using Pandacap.HighLevel.ATProto;
using Pandacap.Clients.ATProto;
using Pandacap.HighLevel;
using BlueskyFeed = Pandacap.Clients.ATProto.BlueskyFeed;

namespace Pandacap.Functions.FavoriteHandlers
{
    public class BlueskyFavoriteHandler(
        PandacapDbContext context,
        ATProtoCredentialProvider credentialProvider,
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

                if (results.NextPage.IsEmpty || results.feed.IsEmpty)
                    break;

                page = results.NextPage.Single();
            }
        }

        /// <summary>
        /// Looks for new Bluesky likes by the attached ATProto account and adds them to the Favorites page.
        /// </summary>
        /// <returns></returns>
        public async Task ImportLikesAsync()
        {
            if (await credentialProvider.GetCrosspostingCredentialsAsync() is not IAutomaticRefreshCredentials credentials)
                return;

            var client = httpClientFactory.CreateClient();
            client.DefaultRequestHeaders.UserAgent.ParseAdd(UserAgentInformation.UserAgent);

            var tooNew = DateTimeOffset.UtcNow.AddMinutes(-5);

            Stack<BlueskyFeed.FeedItem> items = [];

            await foreach (var feedItem in WrapAsync(page => BlueskyFeed.GetActorLikesAsync(client, credentials, credentials.DID, page)))
            {
                if (feedItem.IndexedAt > tooNew)
                    continue;

                var existing = await context.BlueskyLikes
                    .Where(item => item.CID == feedItem.post.cid)
                    .CountAsync();
                if (existing > 0)
                    break;

                bool isAdultContent = feedItem.post.labels
                    .Where(l => l.src == feedItem.post.author.did || l.src == BlueskyModerationService)
                    .Select(l => l.val)
                    .Intersect(BlueskyModerationServiceAdultContentLabels)
                    .Any();
                if (isAdultContent)
                    continue;

                items.Push(feedItem);

                if (items.Count >= 200)
                    break;
            }

            while (items.TryPop(out var feedItem))
            {
                var now = DateTimeOffset.UtcNow;
                var age = now - feedItem.post.record.createdAt;

                context.BlueskyLikes.Add(new()
                {
                    Id = Guid.NewGuid(),
                    CID = feedItem.post.cid,
                    RecordKey = feedItem.post.RecordKey,
                    CreatedBy = new()
                    {
                        DID = feedItem.post.author.did,
                        PDS = await didResolver.GetPDSAsync(feedItem.post.author.did),
                        DisplayName = feedItem.post.author.DisplayNameOrNull,
                        Handle = feedItem.post.author.handle,
                        Avatar = feedItem.post.author.AvatarOrNull
                    },
                    CreatedAt = feedItem.post.record.createdAt,
                    FavoritedAt = DateTime.UtcNow.Date,
                    Text = feedItem.post.record.text,
                    Images = [
                        .. feedItem.post.Images
                            .Select(image => new BlueskyFavoriteImage
                            {
                                Thumb = image.thumb,
                                Fullsize = image.fullsize,
                                Alt = image.alt
                            })
                    ]
                });
            }

            await context.SaveChangesAsync();
        }

        /// <summary>
        /// Looks for new Bluesky reposts by the attached ATProto account and adds them to the Favorites page.
        /// </summary>
        /// <returns></returns>
        public async Task ImportRepostsAsync()
        {
            if (await credentialProvider.GetCrosspostingCredentialsAsync() is not IAutomaticRefreshCredentials credentials)
                return;

            var client = httpClientFactory.CreateClient();
            client.DefaultRequestHeaders.UserAgent.ParseAdd(UserAgentInformation.UserAgent);

            var tooNew = DateTimeOffset.UtcNow.AddMinutes(-5);

            Stack<BlueskyFeed.FeedItem> items = [];

            await foreach (var feedItem in WrapAsync(page => BlueskyFeed.GetAuthorFeedAsync(client, credentials.DID, page)))
            {
                if (feedItem.IndexedAt > tooNew)
                    continue;

                if (feedItem.post.author.did == credentials.DID)
                    continue;

                var existing = await context.BlueskyReposts
                    .Where(item => item.CID == feedItem.post.cid)
                    .CountAsync();
                if (existing > 0)
                    break;

                items.Push(feedItem);

                if (items.Count >= 200)
                    break;
            }

            while (items.TryPop(out var feedItem))
            {
                var now = DateTimeOffset.UtcNow;
                var age = now - feedItem.post.record.createdAt;

                context.BlueskyReposts.Add(new()
                {
                    Id = Guid.NewGuid(),
                    CID = feedItem.post.cid,
                    RecordKey = feedItem.post.RecordKey,
                    CreatedBy = new()
                    {
                        DID = feedItem.post.author.did,
                        PDS = await didResolver.GetPDSAsync(feedItem.post.author.did),
                        DisplayName = feedItem.post.author.DisplayNameOrNull,
                        Handle = feedItem.post.author.handle,
                        Avatar = feedItem.post.author.AvatarOrNull
                    },
                    CreatedAt = feedItem.post.record.createdAt,
                    FavoritedAt = DateTime.UtcNow.Date,
                    Text = feedItem.post.record.text,
                    Images = [
                        .. feedItem.post.Images
                            .Select(image => new BlueskyFavoriteImage
                            {
                                Thumb = image.thumb,
                                Fullsize = image.fullsize,
                                Alt = image.alt
                            })
                    ]
                });

                while (DateTimeOffset.UtcNow == now)
                    await Task.Delay(1);
            }

            await context.SaveChangesAsync();
        }
    }
}
