using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Pandacap.Clients;
using Pandacap.ConfigurationObjects;
using Pandacap.Data;

namespace Pandacap.HighLevel.ATProto
{
    public class ATProtoFeedReader(
        PandacapDbContext context,
        IHttpClientFactory httpClientFactory,
        IMemoryCache memoryCache)
    {
        private Task<string?> GetPDSAsync(HttpClient client, string did) =>
            memoryCache.GetOrCreateAsync(
                $"ATProtoFeedReader.GetPDSAsync.{did}",
                async _ =>
                {
                    var doc = await ATProtoClient.PLCDirectory.ResolveAsync(client, did);
                    return doc.PDS;
                },
                new MemoryCacheEntryOptions
                {
                    AbsoluteExpiration = DateTimeOffset.UtcNow.AddHours(1),
                });

        private record RepostData {
            public required ATProtoClient.Repo.RecordListItem<ATProtoClient.Repo.Schemas.Bluesky.Feed.Post> Original { get; init; }
            public required ATProtoClient.Repo.RecordListItem<ATProtoClient.Repo.Schemas.Bluesky.Feed.Repost> Repost { get; init; }
            public required string OriginalPDS { get; init; }
        }

        private async Task<RepostData?> GetRepostDataAsync(
            HttpClient client,
            ATProtoClient.Repo.RecordListItem<ATProtoClient.Repo.Schemas.Bluesky.Feed.Repost> repost)
        {
            try
            {
                var pds = await GetPDSAsync(client, repost.value.subject.DID);
                if (pds == null)
                    return null;

                var post = await ATProtoClient.Repo.GetBlueskyFeedPostAsync(
                    client,
                    ATProtoClient.Host.Unauthenticated(pds),
                    repost.value.subject.DID,
                    repost.value.subject.RecordKey);

                return new RepostData
                {
                    Original = post,
                    Repost = repost,
                    OriginalPDS = pds
                };
            }
            catch (ATProtoClient.XrpcException)
            {
                return null;
            }
        }

        public async Task RefreshFeedAsync(
            string did)
        {
            var client = httpClientFactory.CreateClient();
            client.DefaultRequestHeaders.UserAgent.ParseAdd(UserAgentInformation.UserAgent);

            var feed = await context.ATProtoFeeds.SingleAsync(f => f.DID == did);

            var credential = ATProtoClient.Host.Unauthenticated(feed.PDS);

            foreach (var trackedCollection in feed.Collections)
            {
                List<string> newItems = [];

                bool isIncluded(
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

                if (trackedCollection.NSID == ATProtoClient.NSIDs.Bluesky.Actor.Profile)
                {
                    await foreach (var profile in ATProtoClient.Repo
                        .EnumerateBlueskyActorProfilesAsync(
                            client,
                            credential,
                            did)
                        .TakeWhile(item => !trackedCollection.LastSeenCIDs.Contains(item.cid))
                        .Take(1))
                    {
                        newItems.Add(profile.cid);

                        feed.DisplayName = profile.value.DisplayName;
                        feed.AvatarCID = profile.value.AvatarCID;
                    }
                }
                else if (trackedCollection.NSID == ATProtoClient.NSIDs.Bluesky.Feed.Post)
                {
                    await foreach (var post in ATProtoClient.Repo
                        .EnumerateBlueskyFeedPostsAsync(
                            client,
                            credential,
                            did)
                        .TakeWhile(item => !trackedCollection.LastSeenCIDs.Contains(item.cid))
                        .Where(isIncluded)
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
                    await foreach (var item in ATProtoClient.Repo
                        .EnumerateBlueskyFeedRepostsAsync(
                            client,
                            credential,
                            did)
                        .TakeWhile(item => !trackedCollection.LastSeenCIDs.Contains(item.cid))
                        .SelectAwait(async item => await GetRepostDataAsync(client, item))
                        .Where(x => x != null && isIncluded(x.Original))
                        .Take(10))
                    {
                        if (item == null)
                            continue;

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

                trackedCollection.LastSeenCIDs = [.. newItems, .. trackedCollection.LastSeenCIDs];
                trackedCollection.LastSeenCIDs = trackedCollection.LastSeenCIDs.Take(5).ToList();
            }

            await context.SaveChangesAsync();
        }
    }
}
