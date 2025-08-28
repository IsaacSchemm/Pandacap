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
                        did))
                    {
                        if (trackedCollection.LastSeenCIDs.Contains(profile.cid))
                        {
                            break;
                        }

                        feed.DisplayName = profile.value.DisplayName;
                        feed.AvatarCID = trackedCollection.Filters.IgnoreImages
                            ? null
                            : profile.value.AvatarCID;

                        trackedCollection.LastSeenCIDs = [profile.cid, ..trackedCollection.LastSeenCIDs.Take(4)];
                    }
                }
                else if (trackedCollection.NSID == ATProtoClient.NSIDs.Bluesky.Feed.Post)
                {
                    await foreach (var post in ATProtoClient.Repo.EnumerateBlueskyFeedPostsAsync(
                        client,
                        ATProtoClient.Host.Unauthenticated(feed.PDS),
                        did))
                    {
                        if (trackedCollection.LastSeenCIDs.Contains(post.cid))
                        {
                            break;
                        }

                        bool isQuotePost = post.value.EmbeddedRecord != null;
                        if (isQuotePost && trackedCollection.Filters.SkipQuotePosts)
                            continue;

                        bool isReply = post.value.InReplyTo != null;
                        if (isReply && trackedCollection.Filters.SkipReplies)
                            continue;

                        //if (trackedCollection.Filters.IgnoreImages)
                        //    throw new NotImplementedException();

                        trackedCollection.LastSeenCIDs = [post.cid, .. trackedCollection.LastSeenCIDs.Take(4)];
                    }
                }
            }

            await context.SaveChangesAsync();
        }
    }
}
