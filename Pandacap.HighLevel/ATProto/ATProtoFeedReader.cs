using Microsoft.EntityFrameworkCore;
using Pandacap.Clients;
using Pandacap.ConfigurationObjects;
using Pandacap.Data;

namespace Pandacap.HighLevel.ATProto
{
    public class ATProtoFeedReader(
        IHttpClientFactory httpClientFactory,
        PandacapDbContext context)
    {
        public async Task RefreshFeedAsync(
            string did)
        {
            var client = httpClientFactory.CreateClient();
            client.DefaultRequestHeaders.UserAgent.ParseAdd(UserAgentInformation.UserAgent);

            var feed = await context.ATProtoFeeds.SingleAsync(f => f.DID == did);

            foreach (var trackedCollection in feed.Collections)
            {
                if (trackedCollection.NSID == ATProtoClient.NSIDs.Bluesky.Actor.Profile)
                {
                    await foreach (var profile in ATProtoClient.Repo.EnumerateBlueskyActorProfilesAsync(
                        client,
                        ATProtoClient.Host.Unauthenticated(feed.PDS),
                        did).Take(1))
                    {
                        if (trackedCollection.LastSeenCIDs.Contains(profile.cid))
                        {
                            break;
                        }

                        feed.DisplayName = profile.value.DisplayName;
                        feed.AvatarCID = profile.value.AvatarCID;

                        trackedCollection.LastSeenCIDs = [profile.cid, ..trackedCollection.LastSeenCIDs.Take(4)];
                    }
                }
                else if (trackedCollection.NSID == ATProtoClient.NSIDs.Bluesky.Feed.Post)
                {
                    await foreach (var post in ATProtoClient.Repo.EnumerateBlueskyFeedPostsAsync(
                        client,
                        ATProtoClient.Host.Unauthenticated(feed.PDS),
                        did).Take(25))
                    {
                        if (trackedCollection.LastSeenCIDs.Contains(post.cid))
                        {
                            break;
                        }

                        bool isQuotePost = post.value.EmbeddedRecord != null;
                        if (isQuotePost && !feed.IncludeQuotePosts)
                            continue;

                        bool isReply = post.value.InReplyTo != null;
                        if (isReply && !feed.IncludeReplies)
                            continue;

                        if (post.value.Images.IsEmpty && !feed.IncludePostsWithoutImages)
                            continue;

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

                        trackedCollection.LastSeenCIDs = [post.cid, .. trackedCollection.LastSeenCIDs.Take(4)];
                    }
                }
            }

            await context.SaveChangesAsync();
        }
    }
}
