using Microsoft.EntityFrameworkCore;
using Microsoft.FSharp.Collections;
using Pandacap.ATProto.Models;
using Pandacap.ATProto.Services.Interfaces;
using Pandacap.Database;
using Pandacap.Ingestion.Interfaces;

namespace Pandacap.Ingestion
{
    internal class ATProtoFeedRefresher(
        IATProtoService atProtoService,
        IBlueskyService blueskyService,
        IDIDResolver didResolver,
        PandacapDbContext pandacapDbContext,
        IStandardSiteService standardSiteService) : IATProtoFeedRefresher
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
            ATProtoRecord<BlueskyInteraction> interaction,
            CancellationToken cancellationToken)
        {
            try
            {
                var doc = await didResolver.ResolveAsync(
                    interaction.Value.Subject.Uri.Components.DID,
                    cancellationToken);

                var subject = await blueskyService.GetPostAsync(
                    doc.PDS,
                    interaction.Value.Subject.Uri.Components.DID,
                    interaction.Value.Subject.Uri.Components.RecordKey,
                    cancellationToken);

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
            pandacapDbContext.BlueskyPostFeedItems.Add(new()
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
                    : [.. post.Value.Images.Select(i => new BlueskyPostFeedItem.Image
                        {
                            Alt = i.Alt,
                            CID = i.CID
                        })],
                RecordKey = post.Ref.Uri.Components.RecordKey,
                Text = post.Value.Text
            });
        }

        private void AddLikeToContext(
            ATProtoFeed feed,
            ATProtoRecord<BlueskyInteraction> like,
            SubjectData subjectData)
        {
            var subject = subjectData.Subject;
            var pds = subjectData.PDS;

            pandacapDbContext.BlueskyLikeFeedItems.Add(new()
            {
                CID = like.Ref.CID,
                CreatedAt = subject.Value.CreatedAt,
                Labels = [.. subject.Value.Labels],
                Images = feed.IgnoreImages
                    ? []
                    : [.. subject.Value.Images.Select(i => new BlueskyRepostFeedItem.Image
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
                LikedAt = like.Value.CreatedAt,
                LikedBy = new()
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

            pandacapDbContext.BlueskyRepostFeedItems.Add(new()
            {
                CID = repost.Ref.CID,
                CreatedAt = subject.Value.CreatedAt,
                Labels = [.. subject.Value.Labels],
                Images = feed.IgnoreImages
                    ? []
                    : [.. subject.Value.Images.Select(i => new BlueskyRepostFeedItem.Image
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

        private void AddToContext(
            ATProtoFeed feed,
            ATProtoRecord<StandardSiteDocument> document)
        {
            pandacapDbContext.StandardSiteDocumentFeedItems.Add(new()
            {
                AddedAt = DateTimeOffset.UtcNow,
                Author = new()
                {
                    AvatarCID = feed.AvatarCID,
                    DID = feed.DID,
                    DisplayName = feed.DisplayName,
                    Handle = feed.Handle,
                    PDS = feed.CurrentPDS
                },
                CID = document.Ref.CID,
                PublishedAt = document.Value.PublishedAt,
                RecordKey = document.Ref.Uri.Components.RecordKey,
                Title = document.Value.Title
            });
        }

        public async Task RefreshAllAsync(CancellationToken cancellationToken)
        {
            var feeds = await pandacapDbContext.ATProtoFeeds
                .Select(f => new { f.DID, f.DisplayName })
                .ToListAsync(cancellationToken);

            List<Exception> exceptions = [];

            foreach (var feed in feeds)
            {
                try
                {
                    await RefreshFeedAsync(feed.DID, cancellationToken);
                }
                catch (Exception ex)
                {
                    exceptions.Add(ex);
                }
            }

            if (exceptions.Count > 0)
                throw new AggregateException(exceptions);
        }

        public async Task AddFeedAsync(string did, CancellationToken cancellationToken)
        {
            var existing = await pandacapDbContext.ATProtoFeeds
                .Where(a => a.DID == did)
                .FirstOrDefaultAsync(cancellationToken);

            if (existing != null)
                return;

            var document = await didResolver.ResolveAsync(did, cancellationToken);

            var collections = await atProtoService.GetCollectionsInRepoAsync(
                document.PDS,
                did,
                cancellationToken);

            pandacapDbContext.ATProtoFeeds.Add(new ATProtoFeed
            {
                DID = did,
                Handle = document.Handle,
                CurrentPDS = document.PDS,
                NSIDs = [
                    .. collections.Intersect([
                        "app.bsky.actor.profile",
                        "app.bsky.feed.post",
                        "app.bsky.feed.repost",
                        "site.standard.document"
                    ])
                ]
            });

            await pandacapDbContext.SaveChangesAsync(cancellationToken);
        }

        public async Task RefreshFeedAsync(
            string did,
            CancellationToken cancellationToken)
        {
            var feed = await pandacapDbContext.ATProtoFeeds.SingleAsync(f => f.DID == did, cancellationToken);

            var doc = await didResolver.ResolveAsync(did, cancellationToken);

            var pds = doc.PDS;

            var cid = await atProtoService.GetLastCommitCIDAsync(
                pds,
                did,
                cancellationToken);

            if (feed.LastCommitCID == cid)
                return;

            feed.LastCommitCID = cid;

            feed.CurrentPDS = doc.PDS;
            feed.Handle = doc.Handle;

            FSharpSet<string> seenLastTime = [.. feed.LastCIDsSeen ?? []];
            List<string> seenThisTime = [];

            var blueskyProfile = await blueskyService.GetProfileAsync(
                pds,
                did,
                cancellationToken);

            if (blueskyProfile is ATProtoRecord<BlueskyProfile> profile)
            {
                feed.DisplayName = profile.Value.DisplayName;
                feed.AvatarCID = profile.Value.AvatarCID;
            }

            if (feed.NSIDs.Contains("app.bsky.feed.like"))
            {
                var likes = blueskyService.GetNewestLikesAsync(
                    pds,
                    did).Take(20).WithCancellation(cancellationToken);

                await foreach (var like in likes)
                {
                    seenThisTime.Add(like.Ref.CID);

                    if (seenLastTime.Contains(like.Ref.CID))
                        continue;

                    var existing = await pandacapDbContext.BlueskyLikeFeedItems.FindAsync(
                        [like.Ref.CID],
                        cancellationToken);

                    if (existing != null)
                        pandacapDbContext.BlueskyLikeFeedItems.Remove(existing);

                    if (await FetchSubjectAsync(like, cancellationToken) is not SubjectData info)
                        continue;

                    if (!PostMatchesFilters(feed, info.Subject))
                        continue;

                    AddLikeToContext(feed, like, info);
                }
            }

            if (feed.NSIDs.Contains("app.bsky.feed.post"))
            {
                var posts = blueskyService.GetNewestPostsAsync(
                    pds,
                    did).Take(20).WithCancellation(cancellationToken);

                await foreach (var post in posts)
                {
                    seenThisTime.Add(post.Ref.CID);

                    if (seenLastTime.Contains(post.Ref.CID))
                        continue;

                    if (!PostMatchesFilters(feed, post))
                        continue;

                    var existing = await pandacapDbContext.BlueskyPostFeedItems.FindAsync(
                        [post.Ref.CID],
                        cancellationToken);
                    if (existing != null)
                        pandacapDbContext.BlueskyPostFeedItems.Remove(existing);

                    AddToContext(feed, post);
                }
            }

            if (feed.NSIDs.Contains("app.bsky.feed.repost"))
            {
                var reposts = blueskyService.GetNewestRepostsAsync(
                    pds,
                    did).Take(20).WithCancellation(cancellationToken);

                await foreach (var repost in reposts)
                {
                    seenThisTime.Add(repost.Ref.CID);

                    if (seenLastTime.Contains(repost.Ref.CID))
                        continue;

                    var existing = await pandacapDbContext.BlueskyRepostFeedItems.FindAsync(
                        [repost.Ref.CID],
                        cancellationToken);
                    if (existing != null)
                        pandacapDbContext.BlueskyRepostFeedItems.Remove(existing);

                    if (await FetchSubjectAsync(repost, cancellationToken) is not SubjectData info)
                        continue;

                    if (!PostMatchesFilters(feed, info.Subject))
                        continue;

                    AddRepostToContext(feed, repost, info);
                }
            }

            if (feed.NSIDs.Contains("site.standard.document"))
            {
                var documents = standardSiteService.GetNewestDocumentsAsync(
                    pds,
                    did).Take(20).WithCancellation(cancellationToken);

                await foreach (var document in documents)
                {
                    seenThisTime.Add(document.Ref.CID);

                    if (seenLastTime.Contains(document.Ref.CID))
                        continue;

                    var existing = await pandacapDbContext.StandardSiteDocumentFeedItems.FindAsync(
                        [document.Ref.CID],
                        cancellationToken);
                    if (existing != null)
                        pandacapDbContext.StandardSiteDocumentFeedItems.Remove(existing);

                    AddToContext(feed, document);
                }
            }

            feed.LastCIDsSeen = [.. seenThisTime];

            await pandacapDbContext.SaveChangesAsync(cancellationToken);
        }
    }
}
