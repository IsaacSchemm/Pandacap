﻿using Microsoft.EntityFrameworkCore;
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

                if (feedItem.post.record.InReplyTo.Length > 0)
                {
                    var inReplyToDIDs =
                        feedItem.post.record.InReplyTo
                        .SelectMany(r => new[] { r.parent, r.root })
                        .Select(r => r.UriComponents.did)
                        .Distinct();

                    if (!inReplyToDIDs.Contains(credentials.DID))
                        continue;
                }

                if (existingPosts.Any(e => e.CID == feedItem.post.cid && e.PostedBy.DID == feedItem.By.did))
                    continue;

                if (feedItem.By.did == credentials.DID)
                    continue;

                context.InboxATProtoPosts.Add(new()
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
