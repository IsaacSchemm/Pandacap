using Microsoft.EntityFrameworkCore;
using Microsoft.FSharp.Core;
using Pandacap.Data;
using Pandacap.LowLevel;
using Pandacap.LowLevel.ATProto;
using System.Linq;

namespace Pandacap.HighLevel
{
    public class ATProtoInboxHandler(
        ApplicationInformation appInfo,
        PandacapDbContext context,
        ATProtoCredentialProvider credentialProvider,
        IHttpClientFactory httpClientFactory)
    {
        private async IAsyncEnumerable<BlueskyFeed.FeedItem> GetTimelineAsync()
        {
            if (await credentialProvider.GetCredentialsAsync() is not IAutomaticRefreshCredentials credentials)
                yield break;

            var page = BlueskyFeed.Page.FromStart;

            var client = httpClientFactory.CreateClient();
            client.DefaultRequestHeaders.UserAgent.ParseAdd(appInfo.UserAgent);

            while (true)
            {
                var results = await BlueskyFeed.GetTimelineAsync(
                    client,
                    credentials,
                    page);

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

            DateTimeOffset someTimeAgo = DateTimeOffset.UtcNow.AddDays(-3);

            var existingPosts = await context.InboxATProtoPosts
                .Where(item => item.IndexedAt >= someTimeAgo)
                .ToListAsync();

            await foreach (var feedItem in GetTimelineAsync())
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
    }
}
