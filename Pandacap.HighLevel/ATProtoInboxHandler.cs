using Microsoft.EntityFrameworkCore;
using Microsoft.FSharp.Core;
using Pandacap.Data;
using Pandacap.LowLevel;
using Pandacap.LowLevel.ATProto;

namespace Pandacap.HighLevel
{
    public class ATProtoInboxHandler(
        ApplicationInformation appInfo,
        PandacapDbContext context,
        ATProtoCredentialProvider credentialProvider,
        IHttpClientFactory httpClientFactory,
        IdMapper idMapper)
    {
        private static async IAsyncEnumerable<BlueskyFeed.FeedItem> WrapAsync(
            Func<BlueskyFeed.Page, Task<BlueskyFeed.FeedResponse>> handler)
        {
            var page = BlueskyFeed.Page.FromStart;

            while (true)
            {
                var results = await handler(page);

                foreach (var item in results.feed)
                    yield return item;

                if (OptionModule.IsNone(results.cursor))
                    break;

                page = BlueskyFeed.Page.NewFromCursor(results.cursor.Value);
            }
        }

        /// <summary>
        /// Imports new posts from the past three days that have not yet been
        /// added to the Pandacap inbox.
        /// </summary>
        /// <returns></returns>
        public async Task ImportPostsByUsersWeWatchAsync()
        {
            if (await credentialProvider.GetCredentialsAsync() is not IAutomaticRefreshCredentials credentials)
                return;

            var client = httpClientFactory.CreateClient();
            client.DefaultRequestHeaders.UserAgent.ParseAdd(appInfo.UserAgent);

            DateTimeOffset someTimeAgo = DateTimeOffset.UtcNow.AddDays(-3);

            var existingPosts = await context.InboxATProtoPosts
                .Where(item => item.IndexedAt >= someTimeAgo)
                .ToListAsync();

            await foreach (var feedItem in WrapAsync(page => BlueskyFeed.GetTimelineAsync(client, credentials, page)))
            {
                if (feedItem.IndexedAt < someTimeAgo)
                    break;

                if (existingPosts.Any(e => e.CID == feedItem.post.cid && e.PostedBy.DID == feedItem.By.did))
                    continue;

                context.InboxATProtoPosts.Add(new()
                {
                    Id = Guid.NewGuid(),
                    CID = feedItem.post.cid,
                    RecordKey = feedItem.post.RecordKey,
                    Author = new()
                    { 
                        DID = feedItem.post.author.did,
                        DisplayName = feedItem.post.author.DisplayNameOrNull,
                        Handle = feedItem.post.author.handle,
                        Avatar = feedItem.post.author.AvatarOrNull
                    },
                    PostedBy = new()
                    {
                        DID = feedItem.By.did,
                        DisplayName = feedItem.By.DisplayNameOrNull,
                        Handle = feedItem.By.handle,
                        Avatar = feedItem.By.AvatarOrNull
                    },
                    CreatedAt = feedItem.post.record.createdAt,
                    IndexedAt = feedItem.IndexedAt,
                    Text = feedItem.post.record.text,
                    Images = feedItem.post.Images
                        .Select(image => new InboxATProtoImage
                        {
                            Thumb = image.thumb,
                            Fullsize = image.fullsize,
                            Alt = image.alt
                        })
                        .ToList()
                });
            }

            await context.SaveChangesAsync();
        }

        /// <summary>
        /// Records the Bluesky app URLs of Pandacap posts that have been bridged by Bridgy Fed.
        /// </summary>
        /// <returns></returns>
        public async Task FindAndRecordBridgedBlueskyUrls()
        {
            var bridgyFedFollows = await context.Follows
                .Where(f => f.ActorId == "https://bsky.brid.gy/bsky.brid.gy")
                .ToListAsync();

            if (bridgyFedFollows.Count == 0)
                return;

            var client = httpClientFactory.CreateClient();
            client.DefaultRequestHeaders.UserAgent.ParseAdd(appInfo.UserAgent);

            var newestPosts = await context.UserPosts
                .OrderByDescending(p => p.PublishedTime)
                .AsAsyncEnumerable()
                .TakeWhile(p => p.BridgedBlueskyUrl == null)
                .ToListAsync();

            if (newestPosts.Count == 0)
                return;

            var cutoff = newestPosts.Select(p => p.PublishedTime).Min();

            string actor = $"{appInfo.Username}.{appInfo.HandleHostname}.ap.brid.gy";

            var upstream =
                await WrapAsync(page => BlueskyFeed.GetAuthorFeedAsync(
                    client,
                    actor,
                    page))
                .TakeWhile(item => item.IndexedAt >= cutoff)
                .ToListAsync();

            foreach (var post in newestPosts)
            {
                if (post.BridgedBlueskyUrl != null)
                    continue;

                string activityPubUrl = idMapper.GetObjectId(post.Id);
                var upstreamItem = upstream.FirstOrDefault(item => item.post.record.OtherUrls.Contains(activityPubUrl));
                if (upstreamItem != null)
                {
                    post.BridgedBlueskyUrl = $"https://bsky.app/profile/{actor}/post/{upstreamItem.post.RecordKey}";
                }
            }

            await context.SaveChangesAsync();
        }
    }
}
