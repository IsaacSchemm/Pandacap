using Pandacap.Clients.ATProto;
using Pandacap.ConfigurationObjects;
using Pandacap.HighLevel;
using Pandacap.HighLevel.ATProto;
using Pandacap.PlatformBadges;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;

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

            List<IAsyncEnumerable<Notification>> enumerables = [];

            foreach (var did in dids)
            {
                enumerables.Add(GetMentionsAsync(did));

                var myDoc = await didResolver.ResolveAsync(did);

                using var client = httpClientFactory.CreateClient();
                client.DefaultRequestHeaders.UserAgent.ParseAdd(UserAgentInformation.UserAgent);

                var myRecentPosts =
                    (await RecordEnumeration.BlueskyPost.FindNewestRecordsAsync(
                        client,
                        XRPC.Host.Unauthenticated(myDoc.PDS),
                        did,
                        20))
                    .Where((post, index) =>
                        index < 5
                        || post.Value.CreatedAt > DateTimeOffset.UtcNow.AddDays(-30));

                enumerables.Add(GetRepliesAsync(myRecentPosts));
                enumerables.Add(GetLikesAsync(myRecentPosts));
                enumerables.Add(GetRepostsAsync(myRecentPosts));
            }

            var combined = enumerables
                .MergeNewest(n => n.Timestamp);

            await foreach (var notification in combined)
                yield return notification;
        }

        private async IAsyncEnumerable<Notification> GetMentionsAsync(
            string did,
            [EnumeratorCancellation]CancellationToken cancellationToken = default)
        {
            using var client = httpClientFactory.CreateClient();
            client.DefaultRequestHeaders.UserAgent.ParseAdd(UserAgentInformation.UserAgent);

            await foreach (var mention in constellationClient.ListMentionsAsync(did, cancellationToken))
            {
                DIDResolverModule.Document? doc = null;
                ATProtoRecord<BlueskyPost>? post = null;

                try
                {
                    doc = await didResolver.ResolveAsync(mention.Components.DID);
                    post = await RecordEnumeration.BlueskyPost.GetRecordAsync(
                        client,
                        XRPC.Host.Unauthenticated(doc.PDS),
                        mention.Components.DID,
                        mention.Components.RecordKey);
                }
                catch (Exception) { }

                yield return new Notification
                {
                    Platform = new NotificationPlatform(
                        "Constellation",
                        PostPlatformModule.GetBadge(PostPlatform.ATProto),
                        $"https://{constellationClient.Host}"),
                    ActivityName = "Mention",
                    Url = $"https://bsky.app/profile/{mention.Components.DID}/post/{mention.Components.RecordKey}",
                    UserName = doc?.Handle ?? mention.Components.DID,
                    UserUrl = $"https://bsky.app/profile/{mention.Components.DID}",
                    PostUrl = $"https://bsky.app/profile/{did}",
                    Timestamp = post?.Value?.CreatedAt ?? DateTimeOffset.UtcNow
                };
            }
        }

        private async IAsyncEnumerable<Notification> GetRepliesAsync(
            IEnumerable<ATProtoRecord<BlueskyPost>> myRecentPosts,
            [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            using var client = httpClientFactory.CreateClient();
            client.DefaultRequestHeaders.UserAgent.ParseAdd(UserAgentInformation.UserAgent);

            foreach (var myPost in myRecentPosts)
            {
                await foreach (var reply in constellationClient.ListRepliesAsync(
                    myPost.Ref.Uri.Components.DID,
                    myPost.Ref.Uri.Components.RecordKey,
                    cancellationToken))
                {
                    DIDResolverModule.Document? doc = null;
                    ATProtoRecord<BlueskyPost>? post = null;

                    try
                    {
                        doc = await didResolver.ResolveAsync(reply.Components.DID);

                        post = await RecordEnumeration.BlueskyPost.GetRecordAsync(
                            client,
                            XRPC.Host.Unauthenticated(doc.PDS),
                            reply.Components.DID,
                            reply.Components.RecordKey);
                    }
                    catch (Exception) { }

                    yield return new Notification
                    {
                        Platform = new NotificationPlatform(
                            "Constellation",
                            PostPlatformModule.GetBadge(PostPlatform.ATProto),
                            $"https://{constellationClient.Host}"),
                        ActivityName = "Reply",
                        Url = $"https://bsky.app/profile/{reply.Components.DID}/post/{reply.Components.RecordKey}",
                        UserName = doc?.Handle ?? reply.Components.DID,
                        UserUrl = $"https://bsky.app/profile/{reply.Components.DID}",
                        PostUrl = $"https://bsky.app/profile/{myPost.Ref.Uri.Components.DID}/post/{myPost.Ref.Uri.Components.RecordKey}",
                        Timestamp = post?.Value?.CreatedAt ?? DateTimeOffset.UtcNow
                    };
                }
            }
        }

        private async IAsyncEnumerable<Notification> GetLikesAsync(
            IEnumerable<ATProtoRecord<BlueskyPost>> myRecentPosts,
            [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            using var client = httpClientFactory.CreateClient();
            client.DefaultRequestHeaders.UserAgent.ParseAdd(UserAgentInformation.UserAgent);

            foreach (var myPost in myRecentPosts)
            {
                await foreach (var like in constellationClient.ListLikesAsync(
                    myPost.Ref.Uri.Components.DID,
                    myPost.Ref.Uri.Components.RecordKey,
                    cancellationToken))
                {
                    DIDResolverModule.Document? doc = null;
                    ATProtoRecord<BlueskyInteraction>? record = null;

                    try
                    {
                        doc = await didResolver.ResolveAsync(like.Components.DID);

                        record = await RecordEnumeration.BlueskyLike.GetRecordAsync(
                            client,
                            XRPC.Host.Unauthenticated(doc.PDS),
                            like.Components.DID,
                            like.Components.RecordKey);
                    }
                    catch (Exception) { }

                    yield return new Notification
                    {
                        Platform = new NotificationPlatform(
                            "Constellation",
                            PostPlatformModule.GetBadge(PostPlatform.ATProto),
                            $"https://{constellationClient.Host}"),
                        ActivityName = "Like",
                        UserName = doc?.Handle ?? like.Components.DID,
                        UserUrl = $"https://bsky.app/profile/{like.Components.DID}",
                        PostUrl = $"https://bsky.app/profile/{myPost.Ref.Uri.Components.DID}/post/{myPost.Ref.Uri.Components.RecordKey}",
                        Timestamp = record?.Value?.CreatedAt ?? DateTimeOffset.UtcNow
                    };
                }
            }
        }

        private async IAsyncEnumerable<Notification> GetRepostsAsync(
            IEnumerable<ATProtoRecord<BlueskyPost>> myRecentPosts,
            [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            using var client = httpClientFactory.CreateClient();
            client.DefaultRequestHeaders.UserAgent.ParseAdd(UserAgentInformation.UserAgent);

            foreach (var myPost in myRecentPosts)
            {
                await foreach (var repost in constellationClient.ListRepostsAsync(
                    myPost.Ref.Uri.Components.DID,
                    myPost.Ref.Uri.Components.RecordKey,
                    cancellationToken))
                {
                    DIDResolverModule.Document? doc = null;
                    ATProtoRecord<BlueskyInteraction>? record = null;

                    try
                    {
                        doc = await didResolver.ResolveAsync(repost.Components.DID);

                        record = await RecordEnumeration.BlueskyRepost.GetRecordAsync(
                            client,
                            XRPC.Host.Unauthenticated(doc.PDS),
                            repost.Components.DID,
                            repost.Components.RecordKey);
                    }
                    catch (Exception) { }

                    yield return new Notification
                    {
                        Platform = new NotificationPlatform(
                            "Constellation",
                            PostPlatformModule.GetBadge(PostPlatform.ATProto),
                            $"https://{constellationClient.Host}"),
                        ActivityName = "Repost",
                        UserName = doc?.Handle ?? repost.Components.DID,
                        UserUrl = $"https://bsky.app/profile/{repost.Components.DID}",
                        PostUrl = $"https://bsky.app/profile/{myPost.Ref.Uri.Components.DID}/post/{myPost.Ref.Uri.Components.RecordKey}",
                        Timestamp = record?.Value?.CreatedAt ?? DateTimeOffset.UtcNow
                    };
                }
            }
        }
    }
}
