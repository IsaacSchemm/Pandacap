using Microsoft.Identity.Client.Platforms.Features.DesktopOs.Kerberos;
using Pandacap.Clients.ATProto;
using Pandacap.ConfigurationObjects;
using Pandacap.HighLevel;
using Pandacap.HighLevel.ATProto;
using Pandacap.PlatformBadges;
using static System.Collections.Specialized.BitVector32;

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
        private const int MAX_PER_TYPE = 25;

        private record Account(
            string DID,
            XRPC.IHost Host);

        public async IAsyncEnumerable<Notification> GetNotificationsAsync()
        {
            foreach (var credential in await atProtoCredentialProvider.GetAllCredentialsAsync())
                await foreach (var notification in GetNotificationsAsync(credential.DID))
                    yield return notification;

            if (await bridgyFedDIDProvider.GetDIDAsync() is string bridgy_did)
                await foreach (var notification in GetNotificationsAsync(bridgy_did))
                    yield return notification;
        }

        public async IAsyncEnumerable<Notification> GetNotificationsAsync(string did)
        {
            var cutoff = DateTimeOffset.UtcNow.AddDays(-75);

            using var client = httpClientFactory.CreateClient();
            client.DefaultRequestHeaders.UserAgent.ParseAdd(UserAgentInformation.UserAgent);

            var myDoc = await didResolver.ResolveAsync(did);

            await foreach (var mention in constellationClient.ListMentionsAsync(
                did,
                CancellationToken.None).Take(MAX_PER_TYPE))
            {
                var doc = await didResolver.ResolveAsync(mention.Components.DID);
                var post = await RecordEnumeration.BlueskyPost.GetRecordAsync(
                    client,
                    XRPC.Host.Unauthenticated(doc.PDS),
                    mention.Components.DID,
                    mention.Components.RecordKey);
                if (post.Value.CreatedAt < cutoff)
                    break;

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
                20);

            var replies = myRecentPosts
                .ToAsyncEnumerable()
                .Where(post => post.Value.CreatedAt >= cutoff)
                .SelectMany(post => constellationClient.ListRepliesAsync(
                    post.Ref.Uri.Components.DID,
                    post.Ref.Uri.Components.RecordKey,
                    CancellationToken.None))
                .Take(MAX_PER_TYPE);

            await foreach (var reply in replies)
            {
                var doc = await didResolver.ResolveAsync(reply.Components.DID);

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
                    PostUrl = $"https://bsky.app/profile/{did}",
                    Timestamp = DateTimeOffset.UtcNow
                };
            }

            //var likes = myRecentPosts.Items
            //    .ToAsyncEnumerable()
            //    .SelectMany(post => constellationClient.ListLikesAsync(
            //        post.Ref.Uri.Components.DID,
            //        post.Ref.Uri.Components.RecordKey,
            //        CancellationToken.None))
            //    .Take(MAX_PER_TYPE);

            //await foreach (var like in likes)
            //{
            //    var doc = await didResolver.ResolveAsync(like.Components.DID);

            //    yield return new Notification
            //    {
            //        Platform = new NotificationPlatform(
            //            "Constellation",
            //            PostPlatformModule.GetBadge(PostPlatform.ATProto),
            //            $"https://{constellationClient.Host}"),
            //        ActivityName = "Like",
            //        UserName = doc.Handle,
            //        UserUrl = $"https://bsky.app/profile/{like.Components.DID}",
            //        PostUrl = $"https://bsky.app/profile/{did}",
            //        Timestamp = DateTimeOffset.UtcNow
            //    };
            //}

            //var reposts = myRecentPosts.Items
            //    .ToAsyncEnumerable()
            //    .SelectMany(post => constellationClient.ListRepostsAsync(
            //        post.Ref.Uri.Components.DID,
            //        post.Ref.Uri.Components.RecordKey,
            //        CancellationToken.None))
            //    .Take(MAX_PER_TYPE);

            //await foreach (var repost in reposts)
            //{
            //    var doc = await didResolver.ResolveAsync(repost.Components.DID);
            //    var post = await XRPC.Com.Atproto.Repo.BlueskyPost.GetRecordAsync(
            //        client,
            //        XRPC.Host.Unauthenticated(doc.PDS),
            //        repost.Components.DID,
            //        repost.Components.RecordKey);

            //    yield return new Notification
            //    {
            //        Platform = new NotificationPlatform(
            //            "Constellation",
            //            PostPlatformModule.GetBadge(PostPlatform.ATProto),
            //            $"https://{constellationClient.Host}"),
            //        ActivityName = "Repost",
            //        UserName = doc.Handle,
            //        UserUrl = $"https://bsky.app/profile/{repost.Components.DID}",
            //        PostUrl = $"https://bsky.app/profile/{did}",
            //        Timestamp = post.Value.CreatedAt
            //    };
            //}
        }
    }
}
