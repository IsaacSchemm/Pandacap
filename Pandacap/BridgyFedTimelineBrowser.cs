using Microsoft.FSharp.Core;
using Pandacap.Data;
using Pandacap.HighLevel;
using Pandacap.JsonLd;
using Pandacap.LowLevel;
using Pandacap.LowLevel.ATProto;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Pandacap
{
    public class BridgyFedTimelineBrowser(
        ActivityPubRemoteActorService activityPubRemoteActorService,
        ApplicationInformation appInfo,
        PandacapDbContext context,
        IHttpClientFactory httpClientFactory,
        ActivityPubTranslator translator)
    {
        private IEnumerable<string> CollectPossibleHandles()
        {
            yield return $"{appInfo.Username}.{appInfo.HandleHostname}.ap.brid.gy";
            yield return $"{appInfo.Username}.{appInfo.ApplicationHostname}.ap.brid.gy";
        }

        private async IAsyncEnumerable<BlueskyFeed.FeedItem> CollectFeedItemsAsync(string handle)
        {
            using var httpClient = httpClientFactory.CreateClient();

            var page = BlueskyFeed.Page.FromStart;
            while (true)
            {
                var feedResponse = await BlueskyFeed.GetAuthorFeedAsync(
                    httpClient,
                    handle,
                    page);

                foreach (var feedItem in feedResponse.feed)
                    yield return feedItem;

                if (!feedResponse.HasNextPage)
                    continue;

                page = feedResponse.NextPage;
            }
        }

        public IAsyncEnumerable<BlueskyFeed.FeedItem> CollectFeedItemsAsync()
        {
            return CollectPossibleHandles()
                .Select(CollectFeedItemsAsync)
                .MergeNewest(f => f.IndexedAt);
        }

        public async Task SendDeletionAsync(BlueskyFeed.FeedItem feedItem, CancellationToken cancellationToken)
        {
            if (OptionModule.ToObj(feedItem.post.record.bridgyOriginalUrl) is not string apId)
                return;

            var actor = await activityPubRemoteActorService.FetchActorAsync(
                "https://bsky.brid.gy",
                cancellationToken);
            context.ActivityPubOutboundActivities.Add(new()
            {
                Id = Guid.NewGuid(),
                Inbox = actor.Inbox,
                JsonBody = ActivityPubSerializer.SerializeWithContext(
                    translator.ObjectToDelete(
                        apId)),
                StoredAt = DateTime.UtcNow
            });
            await context.SaveChangesAsync(cancellationToken);
        }
    }
}
