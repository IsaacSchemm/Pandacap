using Microsoft.EntityFrameworkCore;
using Pandacap.Clients;
using Pandacap.ConfigurationObjects;
using Pandacap.Data;

namespace Pandacap.HighLevel.ATProto
{
    public class ATProtoFeedReader(
        PandacapDbContext context,
        IHttpClientFactory httpClientFactory)
    {
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

        private record SubjectData(
            ATProtoClient.Repo.RecordListItem<ATProtoClient.Repo.Schemas.Bluesky.Feed.Post> Subject,
            string PDS);

        private static async Task<SubjectData?> FetchSubjectAsync<T>(
            HttpClient client,
            ATProtoClient.Repo.RecordListItem<T> repost) where T : ATProtoClient.Repo.Schemas.Bluesky.Feed.IHasSubject
        {
            try
            {
                var doc = await ATProtoClient.PLCDirectory.ResolveAsync(
                    client,
                    repost.value.Subject.DID);

                var subject = await ATProtoClient.Repo.GetRecordAsync<ATProtoClient.Repo.Schemas.Bluesky.Feed.Post>(
                    client,
                    ATProtoClient.Host.Unauthenticated(doc.PDS),
                    repost.value.Subject.DID,
                    ATProtoClient.NSIDs.Bluesky.Feed.Post,
                    repost.value.Subject.RecordKey);

                return new(subject, doc.PDS);
            }
            catch (Exception)
            {
                return null;
            }
        }

        private void AddToContext(
            ATProtoFeed feed,
            ATProtoClient.Repo.RecordListItem<ATProtoClient.Repo.Schemas.Bluesky.Feed.Like> like,
            SubjectData subjectData)
        {
            var subject = subjectData.Subject;
            var pds = subjectData.PDS;

            context.BlueskyRepostFeedItems.Add(new()
            {
                CID = like.cid,
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
                RepostedAt = like.value.createdAt,
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

        private void AddToContext(
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

        private void AddToContext(
            ATProtoFeed feed,
            ATProtoClient.Repo.RecordListItem<ATProtoClient.Repo.Schemas.Bluesky.Feed.Repost> repost,
            SubjectData subjectData)
        {
            var subject = subjectData.Subject;
            var pds = subjectData.PDS;

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

            feed.Cursors ??= [];

            if (feed.NSIDs.Contains(ATProtoClient.NSIDs.Bluesky.Actor.Profile))
            {
                var blueskyProfiles = await ATProtoClient.Repo.ListRecordsAsync<ATProtoClient.Repo.Schemas.Bluesky.Actor.Profile>(
                    client,
                    ATProtoClient.Host.Unauthenticated(feed.PDS),
                    did,
                    ATProtoClient.NSIDs.Bluesky.Actor.Profile,
                    1,
                    null,
                    ATProtoClient.Repo.Direction.Forward);

                foreach (var profile in blueskyProfiles.records)
                {
                    feed.DisplayName = profile.value.DisplayName;
                    feed.AvatarCID = profile.value.AvatarCID;
                }
            }

            if (feed.NSIDs.Contains(ATProtoClient.NSIDs.Bluesky.Feed.Like))
            {
                if (!feed.Cursors.ContainsKey(ATProtoClient.NSIDs.Bluesky.Feed.Like))
                {
                    var page = await ATProtoClient.Repo.ListRecordsAsync<ATProtoClient.Repo.Schemas.Bluesky.Feed.Like>(
                        client,
                        ATProtoClient.Host.Unauthenticated(feed.PDS),
                        did,
                        ATProtoClient.NSIDs.Bluesky.Feed.Like,
                        21,
                        null,
                        ATProtoClient.Repo.Direction.Forward);

                    feed.Cursors[ATProtoClient.NSIDs.Bluesky.Feed.Like] = page.Cursor;
                }

                var cursor = feed.Cursors[ATProtoClient.NSIDs.Bluesky.Feed.Like];

                var likes = await ATProtoClient.Repo.ListRecordsAsync<ATProtoClient.Repo.Schemas.Bluesky.Feed.Like>(
                    client,
                    ATProtoClient.Host.Unauthenticated(feed.PDS),
                    did,
                    ATProtoClient.NSIDs.Bluesky.Feed.Like,
                    100,
                    cursor,
                    ATProtoClient.Repo.Direction.Reverse);

                foreach (var like in likes.records)
                {
                    var existing = await context.BlueskyLikeFeedItems.FindAsync(like.cid);
                    if (existing != null)
                        context.BlueskyLikeFeedItems.Remove(existing);

                    if (await FetchSubjectAsync(client, like) is not SubjectData info)
                        continue;

                    if (!PostMatchesFilters(feed, info.Subject))
                        continue;

                    AddToContext(feed, like, info);
                }

                if (likes.Cursor is string next)
                    feed.Cursors[ATProtoClient.NSIDs.Bluesky.Feed.Like] = next;
            }

            if (feed.NSIDs.Contains(ATProtoClient.NSIDs.Bluesky.Feed.Post))
            {
                if (!feed.Cursors.ContainsKey(ATProtoClient.NSIDs.Bluesky.Feed.Post))
                {
                    var page = await ATProtoClient.Repo.ListRecordsAsync<ATProtoClient.Repo.Schemas.Bluesky.Feed.Post>(
                        client,
                        ATProtoClient.Host.Unauthenticated(feed.PDS),
                        did,
                        ATProtoClient.NSIDs.Bluesky.Feed.Post,
                        21,
                        null,
                        ATProtoClient.Repo.Direction.Forward);

                    feed.Cursors[ATProtoClient.NSIDs.Bluesky.Feed.Post] = page.Cursor;
                }

                var cursor = feed.Cursors[ATProtoClient.NSIDs.Bluesky.Feed.Post];

                var posts = await ATProtoClient.Repo.ListRecordsAsync<ATProtoClient.Repo.Schemas.Bluesky.Feed.Post>(
                    client,
                    ATProtoClient.Host.Unauthenticated(feed.PDS),
                    did,
                    ATProtoClient.NSIDs.Bluesky.Feed.Post,
                    100,
                    cursor,
                    ATProtoClient.Repo.Direction.Reverse);

                foreach (var post in posts.records)
                {
                    if (!PostMatchesFilters(feed, post))
                        continue;

                    var existing = await context.BlueskyPostFeedItems.FindAsync(post.cid);
                    if (existing != null)
                        context.BlueskyPostFeedItems.Remove(existing);

                    AddToContext(feed, post);
                }

                if (posts.Cursor is string next)
                    feed.Cursors[ATProtoClient.NSIDs.Bluesky.Feed.Post] = next;
            }

            if (feed.NSIDs.Contains(ATProtoClient.NSIDs.Bluesky.Feed.Repost))
            {
                if (!feed.Cursors.ContainsKey(ATProtoClient.NSIDs.Bluesky.Feed.Repost))
                {
                    var page = await ATProtoClient.Repo.ListRecordsAsync<ATProtoClient.Repo.Schemas.Bluesky.Feed.Repost>(
                        client,
                        ATProtoClient.Host.Unauthenticated(feed.PDS),
                        did,
                        ATProtoClient.NSIDs.Bluesky.Feed.Repost,
                        21,
                        null,
                        ATProtoClient.Repo.Direction.Forward);

                    feed.Cursors[ATProtoClient.NSIDs.Bluesky.Feed.Repost] = page.Cursor;
                }

                var cursor = feed.Cursors[ATProtoClient.NSIDs.Bluesky.Feed.Repost];

                var reposts = await ATProtoClient.Repo.ListRecordsAsync<ATProtoClient.Repo.Schemas.Bluesky.Feed.Repost>(
                    client,
                    ATProtoClient.Host.Unauthenticated(feed.PDS),
                    did,
                    ATProtoClient.NSIDs.Bluesky.Feed.Repost,
                    100,
                    cursor,
                    ATProtoClient.Repo.Direction.Reverse);

                foreach (var repost in reposts.records)
                {
                    var existing = await context.BlueskyRepostFeedItems.FindAsync(repost.cid);
                    if (existing != null)
                        context.BlueskyRepostFeedItems.Remove(existing);

                    if (await FetchSubjectAsync(client, repost) is not SubjectData info)
                        continue;

                    if (!PostMatchesFilters(feed, info.Subject))
                        continue;

                    AddToContext(feed, repost, info);
                }

                if (reposts.Cursor is string next)
                    feed.Cursors[ATProtoClient.NSIDs.Bluesky.Feed.Repost] = next;
            }

            await context.SaveChangesAsync();
        }
    }
}
