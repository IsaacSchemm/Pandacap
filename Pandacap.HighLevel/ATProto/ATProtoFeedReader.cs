using Microsoft.EntityFrameworkCore;
using Pandacap.Clients.ATProto;
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
            Lexicon.IRecord<Lexicon.App.Bsky.Feed.Post> post)
        {
            bool isQuotePost = post.Value.EmbeddedRecord != null;
            if (isQuotePost && !feed.IncludeQuotePosts)
                return false;

            bool isReply = post.Value.InReplyTo != null;
            if (isReply && !feed.IncludeReplies)
                return false;

            if (post.Value.Images.IsEmpty && !feed.IncludePostsWithoutImages)
                return false;

            return true;
        }

        private record SubjectData(
            Lexicon.IRecord<Lexicon.App.Bsky.Feed.Post> Subject,
            string PDS);

        private static async Task<SubjectData?> FetchSubjectAsync<T>(
            HttpClient client,
            Lexicon.IRecord<T> repost) where T : Lexicon.IHasSubject
        {
            try
            {
                var doc = await DIDResolver.ResolveAsync(
                    client,
                    repost.Value.Subject.DID);

                var subject = await XRPC.Com.Atproto.Repo.GetRecordAsync<Lexicon.App.Bsky.Feed.Post>(
                    client,
                    XRPC.Host.Unauthenticated(doc.PDS),
                    repost.Value.Subject.DID,
                    NSIDs.App.Bsky.Feed.Post,
                    repost.Value.Subject.RecordKey);

                return new(subject, doc.PDS);
            }
            catch (Exception)
            {
                return null;
            }
        }

        private void AddToContext(
            ATProtoFeed feed,
            Lexicon.IRecord<Lexicon.App.Bsky.Feed.Like> like,
            SubjectData subjectData)
        {
            var subject = subjectData.Subject;
            var pds = subjectData.PDS;

            context.BlueskyRepostFeedItems.Add(new()
            {
                CID = like.CID,
                CreatedAt = subject.Value.createdAt,
                Labels = [.. subject.Value.Labels],
                Images = feed.IgnoreImages
                    ? []
                    : [.. subject.Value.Images.Select(i => new BlueskyRepostFeedItemImage
                    {
                        Alt = i.Alt,
                        CID = i.image.CID
                    })],
                Original = new()
                {
                    CID = subject.CID,
                    DID = subject.DID,
                    PDS = pds,
                    RecordKey = subject.RecordKey
                },
                RepostedAt = like.Value.createdAt,
                RepostedBy = new()
                {
                    AvatarCID = feed.AvatarCID,
                    DID = feed.DID,
                    DisplayName = feed.DisplayName,
                    Handle = feed.Handle,
                    PDS = feed.PDS
                },
                Text = subject.Value.text
            });
        }

        private void AddToContext(
            ATProtoFeed feed,
            Lexicon.IRecord<Lexicon.App.Bsky.Feed.Post> post)
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
                CID = post.CID,
                CreatedAt = post.Value.createdAt,
                Labels = [.. post.Value.Labels],
                Images = feed.IgnoreImages
                    ? []
                    : [.. post.Value.Images.Select(i => new BlueskyPostFeedItemImage
                        {
                            Alt = i.Alt,
                            CID = i.image.CID
                        })],
                RecordKey = post.RecordKey,
                Text = post.Value.text
            });
        }

        private void AddToContext(
            ATProtoFeed feed,
            Lexicon.IRecord<Lexicon.App.Bsky.Feed.Repost> repost,
            SubjectData subjectData)
        {
            var subject = subjectData.Subject;
            var pds = subjectData.PDS;

            context.BlueskyRepostFeedItems.Add(new()
            {
                CID = repost.CID,
                CreatedAt = subject.Value.createdAt,
                Labels = [.. subject.Value.Labels],
                Images = feed.IgnoreImages
                    ? []
                    : [.. subject.Value.Images.Select(i => new BlueskyRepostFeedItemImage
                    {
                        Alt = i.Alt,
                        CID = i.image.CID
                    })],
                Original = new()
                {
                    CID = subject.CID,
                    DID = subject.DID,
                    PDS = pds,
                    RecordKey = subject.RecordKey
                },
                RepostedAt = repost.Value.createdAt,
                RepostedBy = new()
                {
                    AvatarCID = feed.AvatarCID,
                    DID = feed.DID,
                    DisplayName = feed.DisplayName,
                    Handle = feed.Handle,
                    PDS = feed.PDS
                },
                Text = subject.Value.text
            });
        }

        public async Task RefreshFeedAsync(
            string did)
        {
            var client = httpClientFactory.CreateClient();
            client.DefaultRequestHeaders.UserAgent.ParseAdd(UserAgentInformation.UserAgent);

            var feed = await context.ATProtoFeeds.SingleAsync(f => f.DID == did);

            feed.Cursors ??= [];

            if (feed.NSIDs.Contains(NSIDs.App.Bsky.Actor.Profile))
            {
                var blueskyProfiles = await XRPC.Com.Atproto.Repo.ListRecordsAsync<Lexicon.App.Bsky.Actor.Profile>(
                    client,
                    XRPC.Host.Unauthenticated(feed.PDS),
                    did,
                    NSIDs.App.Bsky.Actor.Profile,
                    1,
                    null,
                    XRPC.Com.Atproto.Repo.Direction.Forward);

                foreach (var profile in blueskyProfiles.records)
                {
                    feed.DisplayName = profile.value.DisplayName;
                    feed.AvatarCID = profile.value.Avatar.CID;
                }
            }

            if (feed.NSIDs.Contains(NSIDs.App.Bsky.Feed.Like))
            {
                if (!feed.Cursors.ContainsKey(NSIDs.App.Bsky.Feed.Like))
                {
                    var page = await XRPC.Com.Atproto.Repo.ListRecordsAsync<Lexicon.App.Bsky.Feed.Like>(
                        client,
                        XRPC.Host.Unauthenticated(feed.PDS),
                        did,
                        NSIDs.App.Bsky.Feed.Like,
                        21,
                        null,
                        XRPC.Com.Atproto.Repo.Direction.Forward);

                    feed.Cursors[NSIDs.App.Bsky.Feed.Like] = page.Cursor;
                }

                var cursor = feed.Cursors[NSIDs.App.Bsky.Feed.Like];

                var likes = await XRPC.Com.Atproto.Repo.ListRecordsAsync<Lexicon.App.Bsky.Feed.Like>(
                    client,
                    XRPC.Host.Unauthenticated(feed.PDS),
                    did,
                    NSIDs.App.Bsky.Feed.Like,
                    100,
                    cursor,
                    XRPC.Com.Atproto.Repo.Direction.Reverse);

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
                    feed.Cursors[NSIDs.App.Bsky.Feed.Like] = next;
            }

            if (feed.NSIDs.Contains(NSIDs.App.Bsky.Feed.Post))
            {
                if (!feed.Cursors.ContainsKey(NSIDs.App.Bsky.Feed.Post))
                {
                    var page = await XRPC.Com.Atproto.Repo.ListRecordsAsync<Lexicon.App.Bsky.Feed.Post>(
                        client,
                        XRPC.Host.Unauthenticated(feed.PDS),
                        did,
                        NSIDs.App.Bsky.Feed.Post,
                        21,
                        null,
                        XRPC.Com.Atproto.Repo.Direction.Forward);

                    feed.Cursors[NSIDs.App.Bsky.Feed.Post] = page.Cursor;
                }

                var cursor = feed.Cursors[NSIDs.App.Bsky.Feed.Post];

                var posts = await XRPC.Com.Atproto.Repo.ListRecordsAsync<Lexicon.App.Bsky.Feed.Post>(
                    client,
                    XRPC.Host.Unauthenticated(feed.PDS),
                    did,
                    NSIDs.App.Bsky.Feed.Post,
                    100,
                    cursor,
                    XRPC.Com.Atproto.Repo.Direction.Reverse);

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
                    feed.Cursors[NSIDs.App.Bsky.Feed.Post] = next;
            }

            if (feed.NSIDs.Contains(NSIDs.App.Bsky.Feed.Repost))
            {
                if (!feed.Cursors.ContainsKey(NSIDs.App.Bsky.Feed.Repost))
                {
                    var page = await XRPC.Com.Atproto.Repo.ListRecordsAsync<Lexicon.App.Bsky.Feed.Repost>(
                        client,
                        XRPC.Host.Unauthenticated(feed.PDS),
                        did,
                        NSIDs.App.Bsky.Feed.Repost,
                        21,
                        null,
                        XRPC.Com.Atproto.Repo.Direction.Forward);

                    feed.Cursors[NSIDs.App.Bsky.Feed.Repost] = page.Cursor;
                }

                var cursor = feed.Cursors[NSIDs.App.Bsky.Feed.Repost];

                var reposts = await XRPC.Com.Atproto.Repo.ListRecordsAsync<Lexicon.App.Bsky.Feed.Repost>(
                    client,
                    XRPC.Host.Unauthenticated(feed.PDS),
                    did,
                    NSIDs.App.Bsky.Feed.Repost,
                    100,
                    cursor,
                    XRPC.Com.Atproto.Repo.Direction.Reverse);

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
                    feed.Cursors[NSIDs.App.Bsky.Feed.Repost] = next;
            }

            await context.SaveChangesAsync();
        }
    }
}
