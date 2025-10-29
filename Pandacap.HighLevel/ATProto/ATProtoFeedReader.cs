using Microsoft.EntityFrameworkCore;
using Microsoft.FSharp.Collections;
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

                var subject = await RecordEnumeration.BlueskyPost.GetRecordAsync(
                    client,
                    doc.PDS,
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
                    DID = feed.DID,
                    Handle = feed.Handle,
                    PDS = feed.CurrentPDS
                },
                CID = entry.Ref.CID,
                CreatedAt = entry.Value.CreatedAt ?? DateTimeOffset.UtcNow,
                RecordKey = entry.Ref.Uri.Components.RecordKey,
                Title = entry.Value.Title
            });
        }

        private void AddToContext(
            ATProtoFeed feed,
            ATProtoRecord<LeafletPublication> publication,
            ATProtoRecord<LeafletDocument> document)
        {
            context.LeafletDocumentFeedItems.Add(new()
            {
                Publication = new()
                {
                    DID = feed.DID,
                    PDS = feed.CurrentPDS,
                    BasePath = publication.Value.BasePath,
                    Name = publication.Value.Name
                },
                CID = document.Ref.CID,
                CreatedAt = document.Value.PublishedAt,
                RecordKey = document.Ref.Uri.Components.RecordKey,
                Title = document.Value.Title
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

            var feed = await context.ATProtoFeeds.SingleAsync(f => f.DID == did);

            var doc = await didResolver.ResolveAsync(did);

            var pds = doc.PDS;

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

            FSharpSet<string> seenLastTime = [.. feed.LastCIDsSeen ?? []];
            List<string> seenThisTime = [];

            var blueskyProfiles = await RecordEnumeration.BlueskyProfile.ListRecordsAsync(
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

            if (feed.NSIDs.Contains(NSIDs.App.Bsky.Feed.Like))
            {
                var likes = await RecordEnumeration.BlueskyLike.FindNewestRecordsAsync(
                    client,
                    pds,
                    did,
                    pageSize: 20);

                foreach (var like in likes)
                {
                    seenThisTime.Add(like.Ref.CID);

                    if (seenLastTime.Contains(like.Ref.CID))
                        continue;

                    var existing = await context.BlueskyLikeFeedItems.FindAsync(like.Ref.CID);
                    if (existing != null)
                        context.BlueskyLikeFeedItems.Remove(existing);

                    if (await FetchSubjectAsync(client, like) is not SubjectData info)
                        continue;

                    if (!PostMatchesFilters(feed, info.Subject))
                        continue;

                    AddLikeToContext(feed, like, info);
                }
            }

            if (feed.NSIDs.Contains(NSIDs.App.Bsky.Feed.Post))
            {
                var posts = await RecordEnumeration.BlueskyPost.FindNewestRecordsAsync(
                    client,
                    pds,
                    did,
                    pageSize: 20);

                foreach (var post in posts)
                {
                    seenThisTime.Add(post.Ref.CID);

                    if (seenLastTime.Contains(post.Ref.CID))
                        continue;

                    if (!PostMatchesFilters(feed, post))
                        continue;

                    var existing = await context.BlueskyPostFeedItems.FindAsync(post.Ref.CID);
                    if (existing != null)
                        context.BlueskyPostFeedItems.Remove(existing);

                    AddToContext(feed, post);
                }
            }

            if (feed.NSIDs.Contains(NSIDs.App.Bsky.Feed.Repost))
            {
                var reposts = await RecordEnumeration.BlueskyRepost.FindNewestRecordsAsync(
                    client,
                    pds,
                    did,
                    pageSize: 20);

                foreach (var repost in reposts)
                {
                    seenThisTime.Add(repost.Ref.CID);

                    if (seenLastTime.Contains(repost.Ref.CID))
                        continue;

                    var existing = await context.BlueskyRepostFeedItems.FindAsync(repost.Ref.CID);
                    if (existing != null)
                        context.BlueskyRepostFeedItems.Remove(existing);

                    if (await FetchSubjectAsync(client, repost) is not SubjectData info)
                        continue;

                    if (!PostMatchesFilters(feed, info.Subject))
                        continue;

                    AddRepostToContext(feed, repost, info);
                }
            }

            if (feed.NSIDs.Contains(NSIDs.Com.Whtwnd.Blog.Entry))
            {
                var blogEntries = await RecordEnumeration.WhitewindBlogEntry.FindNewestRecordsAsync(
                    client,
                    pds,
                    did,
                    pageSize: 20);

                foreach (var blogEntry in blogEntries)
                {
                    seenThisTime.Add(blogEntry.Ref.CID);

                    if (seenLastTime.Contains(blogEntry.Ref.CID))
                        continue;

                    var existing = await context.WhiteWindBlogEntryFeedItems.FindAsync(blogEntry.Ref.CID);
                    if (existing != null)
                        context.WhiteWindBlogEntryFeedItems.Remove(existing);

                    AddToContext(feed, blogEntry);
                }
            }

            if (feed.NSIDs.Contains(NSIDs.Pub.Leaflet.Document))
            {
                var documents = await RecordEnumeration.LeafletDocument.FindNewestRecordsAsync(
                    client,
                    pds,
                    did,
                    pageSize: 20);

                var publications = await Task.WhenAll(
                    documents
                    .Select(document => document.Value.Publication.Components.RecordKey)
                    .Distinct()
                    .Select(recordKey => RecordEnumeration.LeafletPublication.GetRecordAsync(
                        client,
                        pds,
                        did,
                        recordKey)));

                foreach (var document in documents)
                {
                    var publication = publications.SingleOrDefault(pub => pub.Ref.Uri.Equals(document.Value.Publication));
                    if (publication == null)
                        continue;

                    seenThisTime.Add(document.Ref.CID);

                    if (seenLastTime.Contains(document.Ref.CID))
                        continue;

                    var existing = await context.LeafletDocumentFeedItems.FindAsync(document.Ref.CID);
                    if (existing != null)
                        context.LeafletDocumentFeedItems.Remove(existing);

                    AddToContext(feed, publication, document);
                }
            }

            feed.LastCIDsSeen = [.. seenThisTime];

            await context.SaveChangesAsync();
        }
    }
}
