using Microsoft.EntityFrameworkCore;
using Microsoft.FSharp.Core;
using Pandacap.Clients;
using Pandacap.ConfigurationObjects;
using Pandacap.Data;

namespace Pandacap.HighLevel.ATProto
{
    public class ATProtoFeedReader(
        PandacapDbContext context,
        IHttpClientFactory httpClientFactory)
    {
        private static async IAsyncEnumerable<ATProtoClient.Repo.RecordListItem<T>> EnumerateAsync<T>(
            HttpClient client,
            ATProtoClient.IHost host,
            string did,
            string collection)
        {
            var cursor = FSharpOption<string>.None;

            while (true)
            {
                var page = await ATProtoClient.Repo.ListRecordsAsync<T>(
                    client,
                    host,
                    did,
                    collection,
                    cursor);

                foreach (var item in page.records)
                {
                    yield return item;
                }

                if (page.records.IsEmpty)
                    yield break;

                cursor = page.cursor;
            }
        }

        private static bool PostMatchesFilters(
            ATProtoFeed feed,
            ATProtoClient.Repo.RecordListItem<ATProtoClient.Repo.Schemas.Bluesky.Feed.Post> post)
        {
            bool isQuotePost = post.value.EmbeddedRecord != null;
            if (isQuotePost && !feed.IncludeQuotePosts)
                return false;

            bool isReply = post.value.InReplyTo != null;
            if (isReply && !feed.IncludeReplies)
                return false;

            if (post.value.Images.IsEmpty && !feed.IncludePostsWithoutImages)
                return false;

            return true;
        }

        private record RepostSubject(
            ATProtoClient.Repo.RecordListItem<ATProtoClient.Repo.Schemas.Bluesky.Feed.Post> Subject,
            string PDS);

        private static async Task<RepostSubject?> FetchSubjectAsync(
            HttpClient client,
            ATProtoClient.Repo.RecordListItem<ATProtoClient.Repo.Schemas.Bluesky.Feed.Repost> repost)
        {
            try
            {
                var doc = await ATProtoClient.PLCDirectory.ResolveAsync(
                    client,
                    repost.value.subject.DID);

                var subject = await ATProtoClient.Repo.GetRecordAsync<ATProtoClient.Repo.Schemas.Bluesky.Feed.Post>(
                    client,
                    ATProtoClient.Host.Unauthenticated(doc.PDS),
                    repost.value.subject.DID,
                    ATProtoClient.NSIDs.Bluesky.Feed.Post,
                    repost.value.subject.RecordKey);

                return new(subject, doc.PDS);
            }
            catch (Exception)
            {
                return null;
            }
        }

        private void Add(
            ATProtoFeed feed,
            ATProtoClient.Repo.RecordListItem<ATProtoClient.Repo.Schemas.Bluesky.Feed.Post> post)
        {
            context.BlueskyPostFeedItems.Add(new()
            {
                Author = new()
                {
                    AvatarCID = feed.AvatarCID,
                    DID = feed.DID,
                    DisplayName = feed.DisplayName,
                    Handle = feed.Handle,
                    PDS = feed.PDS
                },
                CID = post.cid,
                CreatedAt = post.value.createdAt,
                Labels = [.. post.value.Labels],
                Images = feed.IgnoreImages
                    ? []
                    : [.. post.value.Images.Select(i => new BlueskyPostFeedItemImage
                        {
                            Alt = i.Alt,
                            CID = i.BlobCID
                        })],
                RecordKey = post.RecordKey,
                Text = post.value.text
            });
        }

        private void Add(
            ATProtoFeed feed,
            ATProtoClient.Repo.RecordListItem<ATProtoClient.Repo.Schemas.Bluesky.Feed.Repost> repost,
            ATProtoClient.Repo.RecordListItem<ATProtoClient.Repo.Schemas.Bluesky.Feed.Post> subject,
            string pds)
        {
            context.BlueskyRepostFeedItems.Add(new()
            {
                CID = repost.cid,
                CreatedAt = subject.value.createdAt,
                Labels = [.. subject.value.Labels],
                Images = feed.IgnoreImages
                    ? []
                    : [.. subject.value.Images.Select(i => new BlueskyRepostFeedItemImage
                    {
                        Alt = i.Alt,
                        CID = i.BlobCID
                    })],
                Original = new()
                {
                    CID = subject.cid,
                    DID = subject.DID,
                    PDS = pds,
                    RecordKey = subject.RecordKey
                },
                RepostedAt = repost.value.createdAt,
                RepostedBy = new()
                {
                    AvatarCID = feed.AvatarCID,
                    DID = feed.DID,
                    DisplayName = feed.DisplayName,
                    Handle = feed.Handle,
                    PDS = feed.PDS
                },
                Text = subject.value.text
            });
        }

        public async Task RefreshFeedAsync(
            string did)
        {
            var client = httpClientFactory.CreateClient();
            client.DefaultRequestHeaders.UserAgent.ParseAdd(UserAgentInformation.UserAgent);

            var feed = await context.ATProtoFeeds.SingleAsync(f => f.DID == did);

            async IAsyncEnumerable<ATProtoClient.Repo.RecordListItem<T>> enumerateAndFilterAsync<T>(
                string nsid,
                int minCount)
            {
                var remaining = minCount;
                var foundKnownItem = false;

                await foreach (var item in EnumerateAsync<T>(
                    client,
                    ATProtoClient.Host.Unauthenticated(feed.PDS),
                    did,
                    nsid))
                {
                    if (foundKnownItem && remaining <= 0)
                        yield break;

                    yield return item;

                    if (remaining > 0)
                        remaining--;

                    foundKnownItem |= feed.LastSeen.Contains(item.cid);
                }
            }

            var isNewFeed = feed.LastSeen.Count == 0;

            var tracker = new List<string>();

            var blueskyProfiles =
                enumerateAndFilterAsync<ATProtoClient.Repo.Schemas.Bluesky.Actor.Profile>(
                    ATProtoClient.NSIDs.Bluesky.Actor.Profile,
                    minCount: 1)
                .Take(1);

            await foreach (var profile in blueskyProfiles)
            {
                tracker.Add(profile.cid);

                if (feed.LastSeen.Contains(profile.cid))
                    continue;

                feed.DisplayName = profile.value.DisplayName;
                feed.AvatarCID = profile.value.AvatarCID;
            }

            var blueskyPosts =
                enumerateAndFilterAsync<ATProtoClient.Repo.Schemas.Bluesky.Feed.Post>(
                    ATProtoClient.NSIDs.Bluesky.Feed.Post,
                    minCount: 5)
                .Take(isNewFeed ? 20 : 100);

            await foreach (var post in blueskyPosts)
            {
                tracker.Add(post.cid);

                if (feed.LastSeen.Contains(post.cid))
                    continue;

                if (!PostMatchesFilters(feed, post))
                    continue;

                var existing = await context.BlueskyPostFeedItems.FindAsync(post.cid);
                if (existing != null)
                    context.BlueskyPostFeedItems.Remove(existing);

                Add(feed, post);
            }

            var blueskyReposts =
                enumerateAndFilterAsync<ATProtoClient.Repo.Schemas.Bluesky.Feed.Repost>(
                    ATProtoClient.NSIDs.Bluesky.Feed.Repost,
                    minCount: 5)
                .Take(isNewFeed ? 20 : 100);

            await foreach (var repost in blueskyReposts)
            {
                tracker.Add(repost.cid);

                if (feed.LastSeen.Contains(repost.cid))
                    continue;

                var existing = await context.BlueskyRepostFeedItems.FindAsync(repost.cid);
                if (existing != null)
                    context.BlueskyRepostFeedItems.Remove(existing);

                if (await FetchSubjectAsync(client, repost) is not RepostSubject info)
                    continue;

                if (!PostMatchesFilters(feed, info.Subject))
                    continue;

                Add(feed, repost, info.Subject, info.PDS);
            }

            feed.LastSeen = tracker;

            await context.SaveChangesAsync();
        }
    }
}
