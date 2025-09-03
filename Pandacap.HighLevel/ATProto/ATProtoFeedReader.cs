using Microsoft.EntityFrameworkCore;
using Pandacap.Clients.ATProto;
using Pandacap.ConfigurationObjects;
using Pandacap.Data;

namespace Pandacap.HighLevel.ATProto
{
    public class ATProtoFeedReader(
        PandacapDbContext context,
        DIDResolver didResolver,
        IHttpClientFactory httpClientFactory)
    {
        private static bool PostMatchesFilters(
            ATProtoFeed feed,
            ATProtoRecord<BlueskyPost> post)
        {
            bool isQuotePost = !post.Value.Quoted.IsEmpty;
            if (isQuotePost && !feed.IncludeQuotePosts)
                return false;

            bool isReply = !post.Value.InReplyTo.IsEmpty;
            if (isReply && !feed.IncludeReplies)
                return false;

            if (post.Value.Images.IsEmpty && !feed.IncludePostsWithoutImages)
                return false;

            return true;
        }

        private record SubjectData(
            ATProtoRecord<BlueskyPost> Subject,
            string PDS);

        private async Task<SubjectData?> FetchSubjectAsync(
            HttpClient client,
            ATProtoRecord<BlueskyInteraction> interaction)
        {
            try
            {
                var doc = await didResolver.ResolveAsync(
                    interaction.Value.Subject.Uri.Components.DID);

                var subject = await XRPC.Com.Atproto.Repo.BlueskyPost.GetRecordAsync(
                    client,
                    XRPC.Host.Unauthenticated(doc.PDS),
                    interaction.Value.Subject.Uri.Components.DID,
                    interaction.Value.Subject.Uri.Components.RecordKey);

                return new(subject, doc.PDS);
            }
            catch (Exception)
            {
                return null;
            }
        }

        private void AddToContext(
            ATProtoFeed feed,
            ATProtoRecord<BlueskyPost> post)
        {
            context.BlueskyPostFeedItems.Add(new()
            {
                Author = new()
                {
                    AvatarCID = feed.AvatarCID,
                    DID = feed.DID,
                    DisplayName = feed.DisplayName,
                    Handle = feed.Handle,
                    PDS = feed.CurrentPDS
                },
                CID = post.Ref.CID,
                CreatedAt = post.Value.CreatedAt,
                Labels = [.. post.Value.Labels],
                Images = feed.IgnoreImages
                    ? []
                    : [.. post.Value.Images.Select(i => new BlueskyPostFeedItemImage
                        {
                            Alt = i.Alt,
                            CID = i.CID
                        })],
                RecordKey = post.Ref.Uri.Components.RecordKey,
                Text = post.Value.Text
            });
        }

        private void AddToContext(
            ATProtoFeed feed,
            ATProtoRecord<WhitewindBlogEntry> entry)
        {
            context.WhiteWindBlogEntryFeedItems.Add(new()
            {
                Author = new()
                {
                    AvatarCID = feed.AvatarCID,
                    DID = feed.DID,
                    DisplayName = feed.DisplayName,
                    Handle = feed.Handle,
                    PDS = feed.CurrentPDS
                },
                CID = entry.Ref.CID,
                Content = entry.Value.Content,
                CreatedAt = entry.Value.CreatedAt ?? DateTimeOffset.UtcNow,
                RecordKey = entry.Ref.Uri.Components.RecordKey,
                Title = entry.Value.Title
            });
        }

        private void AddLikeToContext(
            ATProtoFeed feed,
            ATProtoRecord<BlueskyInteraction> like,
            SubjectData subjectData)
        {
            var subject = subjectData.Subject;
            var pds = subjectData.PDS;

            context.BlueskyRepostFeedItems.Add(new()
            {
                CID = like.Ref.CID,
                CreatedAt = subject.Value.CreatedAt,
                Labels = [.. subject.Value.Labels],
                Images = feed.IgnoreImages
                    ? []
                    : [.. subject.Value.Images.Select(i => new BlueskyRepostFeedItemImage
                    {
                        Alt = i.Alt,
                        CID = i.CID
                    })],
                Original = new()
                {
                    CID = subject.Ref.CID,
                    DID = subject.Ref.Uri.Components.DID,
                    PDS = pds,
                    RecordKey = subject.Ref.Uri.Components.RecordKey
                },
                RepostedAt = like.Value.CreatedAt,
                RepostedBy = new()
                {
                    AvatarCID = feed.AvatarCID,
                    DID = feed.DID,
                    DisplayName = feed.DisplayName,
                    Handle = feed.Handle,
                    PDS = feed.CurrentPDS
                },
                Text = subject.Value.Text
            });
        }

        private void AddRepostToContext(
            ATProtoFeed feed,
            ATProtoRecord<BlueskyInteraction> repost,
            SubjectData subjectData)
        {
            var subject = subjectData.Subject;
            var pds = subjectData.PDS;

            context.BlueskyRepostFeedItems.Add(new()
            {
                CID = repost.Ref.CID,
                CreatedAt = subject.Value.CreatedAt,
                Labels = [.. subject.Value.Labels],
                Images = feed.IgnoreImages
                    ? []
                    : [.. subject.Value.Images.Select(i => new BlueskyRepostFeedItemImage
                    {
                        Alt = i.Alt,
                        CID = i.CID
                    })],
                Original = new()
                {
                    CID = subject.Ref.CID,
                    DID = subject.Ref.Uri.Components.DID,
                    PDS = pds,
                    RecordKey = subject.Ref.Uri.Components.RecordKey
                },
                RepostedAt = repost.Value.CreatedAt,
                RepostedBy = new()
                {
                    AvatarCID = feed.AvatarCID,
                    DID = feed.DID,
                    DisplayName = feed.DisplayName,
                    Handle = feed.Handle,
                    PDS = feed.CurrentPDS
                },
                Text = subject.Value.Text
            });
        }

        public async Task RefreshFeedAsync(
            string did)
        {
            var client = httpClientFactory.CreateClient();
            client.DefaultRequestHeaders.UserAgent.ParseAdd(UserAgentInformation.UserAgent);

            var feed = await context.ATProtoFeeds.SingleAsync(f => f.DID == did);
            feed.Cursors ??= [];

            var doc = await didResolver.ResolveAsync(did);

            var pds = XRPC.Host.Unauthenticated(doc.PDS);

            var commit = await XRPC.Com.Atproto.Repo.GetLatestCommitAsync(
                client,
                pds,
                did);

            if (feed.LastCommitCID == commit.cid)
            {
                return;
            }

            feed.LastCommitCID = commit.cid;

            feed.CurrentPDS = doc.PDS;
            feed.Handle = doc.Handle;

            if (feed.NSIDs.Contains(NSIDs.App.Bsky.Actor.Profile))
            {
                var blueskyProfiles = await XRPC.Com.Atproto.Repo.BlueskyProfile.ListRecordsAsync(
                    client,
                    pds,
                    did,
                    1,
                    null,
                    ATProtoListDirection.Forward);

                foreach (var profile in blueskyProfiles.Items)
                {
                    feed.DisplayName = profile.Value.DisplayName;
                    feed.AvatarCID = profile.Value.AvatarCID;
                }
            }

            if (feed.NSIDs.Contains(NSIDs.App.Bsky.Feed.Like))
            {
                if (!feed.Cursors.TryGetValue(NSIDs.App.Bsky.Feed.Like, out var _))
                {
                    var page = await XRPC.Com.Atproto.Repo.BlueskyLike.ListRecordsAsync(
                        client,
                        pds,
                        did,
                        21,
                        null,
                        ATProtoListDirection.Forward);

                    feed.Cursors[NSIDs.App.Bsky.Feed.Like] = page.Cursor;
                }

                var cursor = feed.Cursors[NSIDs.App.Bsky.Feed.Like];

                var likes = await XRPC.Com.Atproto.Repo.BlueskyLike.ListRecordsAsync(
                    client,
                    pds,
                    did,
                    100,
                    cursor,
                    ATProtoListDirection.Reverse);

                foreach (var like in likes.Items)
                {
                    var existing = await context.BlueskyLikeFeedItems.FindAsync(like.Ref.CID);
                    if (existing != null)
                        context.BlueskyLikeFeedItems.Remove(existing);

                    if (await FetchSubjectAsync(client, like) is not SubjectData info)
                        continue;

                    if (!PostMatchesFilters(feed, info.Subject))
                        continue;

                    AddLikeToContext(feed, like, info);
                }

                if (likes.Cursor is string next)
                    feed.Cursors[NSIDs.App.Bsky.Feed.Like] = next;
            }

            if (feed.NSIDs.Contains(NSIDs.App.Bsky.Feed.Post))
            {
                if (!feed.Cursors.TryGetValue(NSIDs.App.Bsky.Feed.Post, out var _))
                {
                    var page = await XRPC.Com.Atproto.Repo.BlueskyPost.ListRecordsAsync(
                        client,
                        pds,
                        did,
                        21,
                        null,
                        ATProtoListDirection.Forward);

                    feed.Cursors[NSIDs.App.Bsky.Feed.Post] = page.Cursor;
                }

                var cursor = feed.Cursors[NSIDs.App.Bsky.Feed.Post];

                var posts = await XRPC.Com.Atproto.Repo.BlueskyPost.ListRecordsAsync(
                    client,
                    pds,
                    did,
                    100,
                    cursor,
                    ATProtoListDirection.Reverse);

                foreach (var post in posts.Items)
                {
                    if (!PostMatchesFilters(feed, post))
                        continue;

                    var existing = await context.BlueskyPostFeedItems.FindAsync(post.Ref.CID);
                    if (existing != null)
                        context.BlueskyPostFeedItems.Remove(existing);

                    AddToContext(feed, post);
                }

                if (posts.Cursor is string next)
                    feed.Cursors[NSIDs.App.Bsky.Feed.Post] = next;
            }

            if (feed.NSIDs.Contains(NSIDs.App.Bsky.Feed.Repost))
            {
                if (!feed.Cursors.TryGetValue(NSIDs.App.Bsky.Feed.Repost, out var _))
                {
                    var page = await XRPC.Com.Atproto.Repo.BlueskyRepost.ListRecordsAsync(
                        client,
                        pds,
                        did,
                        21,
                        null,
                        ATProtoListDirection.Forward);

                    feed.Cursors[NSIDs.App.Bsky.Feed.Repost] = page.Cursor;
                }

                var cursor = feed.Cursors[NSIDs.App.Bsky.Feed.Repost];

                var reposts = await XRPC.Com.Atproto.Repo.BlueskyRepost.ListRecordsAsync(
                    client,
                    pds,
                    did,
                    100,
                    cursor,
                    ATProtoListDirection.Reverse);

                foreach (var repost in reposts.Items)
                {
                    var existing = await context.BlueskyRepostFeedItems.FindAsync(repost.Ref.CID);
                    if (existing != null)
                        context.BlueskyRepostFeedItems.Remove(existing);

                    if (await FetchSubjectAsync(client, repost) is not SubjectData info)
                        continue;

                    if (!PostMatchesFilters(feed, info.Subject))
                        continue;

                    AddRepostToContext(feed, repost, info);
                }

                if (reposts.Cursor is string next)
                    feed.Cursors[NSIDs.App.Bsky.Feed.Repost] = next;
            }

            if (feed.NSIDs.Contains(NSIDs.Com.Whtwnd.Blog.Entry))
            {
                if (!feed.Cursors.TryGetValue(NSIDs.Com.Whtwnd.Blog.Entry, out var _))
                {
                    var page = await XRPC.Com.Atproto.Repo.WhitewindBlogEntry.ListRecordsAsync(
                        client,
                        pds,
                        did,
                        21,
                        null,
                        ATProtoListDirection.Forward);

                    feed.Cursors[NSIDs.Com.Whtwnd.Blog.Entry] = page.Cursor;
                }

                var cursor = feed.Cursors[NSIDs.Com.Whtwnd.Blog.Entry];

                var blogEntries = await XRPC.Com.Atproto.Repo.WhitewindBlogEntry.ListRecordsAsync(
                    client,
                    pds,
                    did,
                    100,
                    cursor,
                    ATProtoListDirection.Reverse);

                foreach (var blogEntry in blogEntries.Items)
                {
                    var existing = await context.WhiteWindBlogEntryFeedItems.FindAsync(blogEntry.Ref.CID);
                    if (existing != null)
                        context.WhiteWindBlogEntryFeedItems.Remove(existing);

                    AddToContext(feed, blogEntry);
                }

                if (blogEntries.Cursor is string next)
                    feed.Cursors[NSIDs.Com.Whtwnd.Blog.Entry] = next;
            }

            await context.SaveChangesAsync();
        }
    }
}
