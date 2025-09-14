using Pandacap.Clients.ATProto;
using Pandacap.ConfigurationObjects;
using Pandacap.HighLevel;
using Pandacap.HighLevel.ATProto;
using Pandacap.PlatformBadges;

namespace Pandacap.Notifications
{
    public class ConstellationNotificationHandler(
        ATProtoCredentialProvider atProtoCredentialProvider,
        BridgyFedDIDProvider bridgyFedDIDProvider,
        ConstellationClient constellationClient,
        DIDResolver didResolver,
        IHttpClientFactory httpClientFactory
    ) : INotificationHandler
    {
        public async IAsyncEnumerable<Notification> GetNotificationsAsync()
        {
            HashSet<string> dids = [];

            foreach (var credential in await atProtoCredentialProvider.GetAllCredentialsAsync())
                dids.Add(credential.DID);

            if (await bridgyFedDIDProvider.GetDIDAsync() is string bridgy_did)
                dids.Add(bridgy_did);

            var combined = dids
                .ToAsyncEnumerable()
                .SelectMany(GetNotificationsAsync)
                .OrderByDescending(n => n.Timestamp);

            await foreach (var notification in combined)
                yield return notification;
        }

        public async IAsyncEnumerable<Notification> GetNotificationsAsync(string did)
        {
            using var client = httpClientFactory.CreateClient();
            client.DefaultRequestHeaders.UserAgent.ParseAdd(UserAgentInformation.UserAgent);

            var myDoc = await didResolver.ResolveAsync(did);

            await foreach (var mention in constellationClient.ListMentionsAsync(
                did,
                CancellationToken.None).Take(10))
            {
                var doc = await didResolver.ResolveAsync(mention.Components.DID);
                var post = await RecordEnumeration.BlueskyPost.GetRecordAsync(
                    client,
                    XRPC.Host.Unauthenticated(doc.PDS),
                    mention.Components.DID,
                    mention.Components.RecordKey);

                yield return new Notification
                {
                    Platform = new NotificationPlatform(
                        "Constellation",
                        PostPlatformModule.GetBadge(PostPlatform.ATProto),
                        $"https://{constellationClient.Host}"),
                    ActivityName = "Mention",
                    Url = $"https://bsky.app/profile/{mention.Components.DID}/post/{mention.Components.RecordKey}",
                    UserName = doc.Handle,
                    UserUrl = $"https://bsky.app/profile/{mention.Components.DID}",
                    PostUrl = $"https://bsky.app/profile/{did}",
                    Timestamp = post.Value.CreatedAt
                };
            }

            var myRecentPosts = await RecordEnumeration.BlueskyPost.FindNewestRecordsAsync(
                client,
                XRPC.Host.Unauthenticated(myDoc.PDS),
                did,
                10);

            myRecentPosts = [.. myRecentPosts.Where(post => post.Value.CreatedAt > DateTimeOffset.UtcNow.AddDays(-30))];

            var replies = myRecentPosts
                .ToAsyncEnumerable()
                .SelectMany(post => constellationClient.ListRepliesAsync(
                    post.Ref.Uri.Components.DID,
                    post.Ref.Uri.Components.RecordKey,
                    CancellationToken.None).Take(10));

            await foreach (var reply in replies)
            {
                var doc = await didResolver.ResolveAsync(reply.Components.DID);

                var record = await RecordEnumeration.BlueskyPost.GetRecordAsync(
                    client,
                    XRPC.Host.Unauthenticated(doc.PDS),
                    reply.Components.DID,
                    reply.Components.RecordKey);

                yield return new Notification
                {
                    Platform = new NotificationPlatform(
                        "Constellation",
                        PostPlatformModule.GetBadge(PostPlatform.ATProto),
                        $"https://{constellationClient.Host}"),
                    ActivityName = "Reply",
                    Url = $"https://bsky.app/profile/{reply.Components.DID}/post/{reply.Components.RecordKey}",
                    UserName = doc.Handle,
                    UserUrl = $"https://bsky.app/profile/{reply.Components.DID}",
                    PostUrl = $"https://bsky.app/profile/{did}/post/{record.Value.InReplyTo[0].Parent.Uri.Components.RecordKey}",
                    Timestamp = DateTimeOffset.UtcNow
                };
            }

            var likes = myRecentPosts
                .ToAsyncEnumerable()
                .SelectMany(post => constellationClient.ListLikesAsync(
                    post.Ref.Uri.Components.DID,
                    post.Ref.Uri.Components.RecordKey,
                    CancellationToken.None).Take(10));

            await foreach (var like in likes)
            {
                var doc = await didResolver.ResolveAsync(like.Components.DID);

                var record = await RecordEnumeration.BlueskyLike.GetRecordAsync(
                    client,
                    XRPC.Host.Unauthenticated(doc.PDS),
                    like.Components.DID,
                    like.Components.RecordKey);

                yield return new Notification
                {
                    Platform = new NotificationPlatform(
                        "Constellation",
                        PostPlatformModule.GetBadge(PostPlatform.ATProto),
                        $"https://{constellationClient.Host}"),
                    ActivityName = "Like",
                    UserName = doc.Handle,
                    UserUrl = $"https://bsky.app/profile/{like.Components.DID}",
                    PostUrl = $"https://bsky.app/profile/{did}/post/{record.Value.Subject.Uri.Components.RecordKey}",
                    Timestamp = record.Value.CreatedAt
                };
            }

            var reposts = myRecentPosts
                .ToAsyncEnumerable()
                .SelectMany(post => constellationClient.ListRepostsAsync(
                    post.Ref.Uri.Components.DID,
                    post.Ref.Uri.Components.RecordKey,
                    CancellationToken.None).Take(10));

            await foreach (var repost in reposts)
            {
                var doc = await didResolver.ResolveAsync(repost.Components.DID);

                var record = await RecordEnumeration.BlueskyLike.GetRecordAsync(
                    client,
                    XRPC.Host.Unauthenticated(doc.PDS),
                    repost.Components.DID,
                    repost.Components.RecordKey);

                yield return new Notification
                {
                    Platform = new NotificationPlatform(
                        "Constellation",
                        PostPlatformModule.GetBadge(PostPlatform.ATProto),
                        $"https://{constellationClient.Host}"),
                    ActivityName = "Repost",
                    UserName = doc.Handle,
                    UserUrl = $"https://bsky.app/profile/{repost.Components.DID}",
                    PostUrl = $"https://bsky.app/profile/{did}/post/{record.Value.Subject.Uri.Components.RecordKey}",
                    Timestamp = record.Value.CreatedAt
                };
            }
        }
    }
}
