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
        public async Task RefreshFeedAsync(
            string did)
        {
            var client = httpClientFactory.CreateClient();
            client.DefaultRequestHeaders.UserAgent.ParseAdd(UserAgentInformation.UserAgent);

            var feed = await context.ATProtoFeeds.SingleAsync(f => f.DID == did);

            bool postMatchesFilters(
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

            var credential = ATProtoClient.Host.Unauthenticated(feed.PDS);

            foreach (var trackedCollection in feed.Collections)
            {
                List<string> newItems = [];

                if (trackedCollection.NSID == ATProtoClient.NSIDs.Bluesky.Actor.Profile)
                {
                    await foreach (var profile in ATProtoClient.Repo.AsynchronousEnumeration
                        .EnumerateBlueskyActorProfilesAsync(
                            client,
                            credential,
                            did,
                            until: trackedCollection.LastSeenCIDs)
                        .Take(1))
                    {
                        newItems.Add(profile.cid);

                        feed.DisplayName = profile.value.DisplayName;
                        feed.AvatarCID = profile.value.AvatarCID;
                    }
                }
                else if (trackedCollection.NSID == ATProtoClient.NSIDs.Bluesky.Feed.Post)
                {
                    await foreach (var post in ATProtoClient.Repo.AsynchronousEnumeration
                        .EnumerateBlueskyFeedPostsAsync(
                            client,
                            credential,
                            did,
                            until: trackedCollection.LastSeenCIDs)
                        .Where(postMatchesFilters)
                        .Take(10))
                    {
                        newItems.Add(post.cid);

                        var existing = await context.BlueskyPostFeedItems.FindAsync(post.cid);
                        if (existing != null)
                            context.BlueskyPostFeedItems.Remove(existing);

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
                }
                else if (trackedCollection.NSID == ATProtoClient.NSIDs.Bluesky.Feed.Repost)
                {
                    await foreach (var item in ATProtoClient.Repo.AsynchronousEnumeration
                        .EnumerateBlueskyFeedRepostsAsync(
                            client,
                            credential,
                            did,
                            until: trackedCollection.LastSeenCIDs)
                        .Where(x => postMatchesFilters(x.Original))
                        .Take(10))
                    {
                        newItems.Add(item.Repost.cid);

                        var existing = await context.BlueskyRepostFeedItems.FindAsync(item.Repost.cid);
                        if (existing != null)
                            context.BlueskyRepostFeedItems.Remove(existing);

                        context.BlueskyRepostFeedItems.Add(new()
                        {
                            CID = item.Repost.cid,
                            CreatedAt = item.Original.value.createdAt,
                            Labels = [.. item.Original.value.Labels],
                            Images = feed.IgnoreImages
                                ? []
                                : [.. item.Original.value.Images.Select(i => new BlueskyRepostFeedItemImage
                                {
                                    Alt = i.Alt,
                                    CID = i.BlobCID
                                })],
                            Original = new()
                            {
                                CID = item.Original.cid,
                                DID = item.Original.DID,
                                PDS = item.OriginalPDS,
                                RecordKey = item.Original.RecordKey
                            },
                            RepostedAt = item.Repost.value.createdAt,
                            RepostedBy = new()
                            {
                                AvatarCID = feed.AvatarCID,
                                DID = feed.DID,
                                DisplayName = feed.DisplayName,
                                Handle = feed.Handle,
                                PDS = feed.PDS
                            },
                            Text = item.Original.value.text
                        });
                    }
                }

                trackedCollection.LastSeenCIDs = [..
                    Enumerable.Empty<string>()
                    .Concat(newItems)
                    .Concat(trackedCollection.LastSeenCIDs)
                    .Take(5)
                ];
            }

            await context.SaveChangesAsync();
        }
    }
}
