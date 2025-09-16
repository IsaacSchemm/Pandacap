using Microsoft.EntityFrameworkCore;
using Pandacap.Clients.ATProto;
using Pandacap.ConfigurationObjects;
using Pandacap.Data;
using Pandacap.HighLevel.ATProto;
using Pandacap.PlatformBadges;

namespace Pandacap.Notifications
{
    public class ATProtoNotificationHandler(
        ATProtoCredentialProvider atProtoCredentialProvider,
        BridgyFedDIDProvider bridgyFedDIDProvider,
        DIDResolver didResolver,
        IHttpClientFactory httpClientFactory,
        PandacapDbContext context
    ) : INotificationHandler
    {
        public async IAsyncEnumerable<Notification> GetNotificationsAsync()
        {
            HashSet<string> dids = [];

            foreach (var credential in await atProtoCredentialProvider.GetAllCredentialsAsync())
                dids.Add(credential.DID);

            if (await bridgyFedDIDProvider.GetDIDAsync() is string bridgy_did)
                dids.Add(bridgy_did);

            using var client = httpClientFactory.CreateClient();
            client.DefaultRequestHeaders.UserAgent.ParseAdd(UserAgentInformation.UserAgent);

            await foreach (var backlink in context.ATProtoBackLinks
                .OrderByDescending(link => link.SeenAt)
                .AsAsyncEnumerable()
                .OrderByDescending(link => link.SeenAt)
                .ThenByDescending(link => link.RecordKey))
            {
                switch ((backlink.Collection, backlink.Path))
                {
                    case ("app.bsky.feed.post", ".facets[app.bsky.richtext.facet].features[app.bsky.richtext.facet#mention].did"):
                        {
                            DIDResolverModule.Document? doc = null;

                            try
                            {
                                doc = await didResolver.ResolveAsync(backlink.DID);
                            }
                            catch (Exception) { }

                            yield return new Notification
                            {
                                Platform = new NotificationPlatform(
                                    "ATProto",
                                    PostPlatformModule.GetBadge(PostPlatform.ATProto),
                                    viewAllUrl: null),
                                ActivityName = "Mention",
                                Url = $"https://bsky.app/profile/{backlink.DID}/post/{backlink.RecordKey}",
                                UserName = doc?.Handle ?? backlink.DID,
                                UserUrl = $"https://bsky.app/profile/{backlink.DID}",
                                PostUrl = $"https://bsky.app/profile/{backlink.Target}",
                                Timestamp = backlink.SeenAt
                            };
                        }

                        break;

                    case ("app.bsky.graph.follow", ".subject"):
                        {
                            DIDResolverModule.Document? doc = null;

                            try
                            {
                                doc = await didResolver.ResolveAsync(backlink.DID);
                            }
                            catch (Exception) { }

                            yield return new Notification
                            {
                                Platform = new NotificationPlatform(
                                    "ATProto",
                                    PostPlatformModule.GetBadge(PostPlatform.ATProto),
                                    viewAllUrl: null),
                                ActivityName = "Follow",
                                UserName = doc?.Handle ?? backlink.DID,
                                UserUrl = $"https://bsky.app/profile/{backlink.DID}",
                                PostUrl = $"https://bsky.app/profile/{backlink.Target}",
                                Timestamp = backlink.SeenAt
                            };
                        }

                        break;

                    case ("app.bsky.feed.post", ".reply.parent.uri"):
                        {
                            ATProtoRefUri myPostUri = new(backlink.Target);

                            DIDResolverModule.Document? theirDoc = null;

                            try
                            {
                                theirDoc = await didResolver.ResolveAsync(backlink.DID);
                            }
                            catch (Exception) { }

                            yield return new Notification
                            {
                                Platform = new NotificationPlatform(
                                    "ATProto",
                                    PostPlatformModule.GetBadge(PostPlatform.ATProto),
                                    viewAllUrl: null),
                                ActivityName = "Reply",
                                Url = $"https://bsky.app/profile/{backlink.DID}/post/{backlink.RecordKey}",
                                UserName = theirDoc?.Handle ?? backlink.DID,
                                UserUrl = $"https://bsky.app/profile/{backlink.DID}",
                                PostUrl = $"https://bsky.app/profile/{myPostUri.Components.DID}/post/{myPostUri.Components.RecordKey}",
                                Timestamp = backlink.SeenAt
                            };
                        }

                        break;

                    case ("app.bsky.feed.like", ".subject.uri"):
                        {
                            ATProtoRefUri myPostUri = new(backlink.Target);

                            DIDResolverModule.Document? theirDoc = null;

                            try
                            {
                                theirDoc = await didResolver.ResolveAsync(backlink.DID);
                            }
                            catch (Exception) { }

                            yield return new Notification
                            {
                                Platform = new NotificationPlatform(
                                    "ATProto",
                                    PostPlatformModule.GetBadge(PostPlatform.ATProto),
                                    viewAllUrl: null),
                                ActivityName = "Like",
                                UserName = theirDoc?.Handle ?? backlink.DID,
                                UserUrl = $"https://bsky.app/profile/{backlink.DID}",
                                PostUrl = $"https://bsky.app/profile/{myPostUri.Components.DID}/post/{myPostUri.Components.RecordKey}",
                                Timestamp = backlink.SeenAt
                            };
                        }

                        break;

                    case ("app.bsky.feed.repost", ".subject.uri"):
                        {
                            ATProtoRefUri myPostUri = new(backlink.Target);

                            DIDResolverModule.Document? theirDoc = null;

                            try
                            {
                                theirDoc = await didResolver.ResolveAsync(backlink.DID);
                            }
                            catch (Exception) { }

                            yield return new Notification
                            {
                                Platform = new NotificationPlatform(
                                    "ATProto",
                                    PostPlatformModule.GetBadge(PostPlatform.ATProto),
                                    viewAllUrl: null),
                                ActivityName = "Repost",
                                UserName = theirDoc?.Handle ?? backlink.DID,
                                UserUrl = $"https://bsky.app/profile/{backlink.DID}",
                                PostUrl = $"https://bsky.app/profile/{myPostUri.Components.DID}/post/{myPostUri.Components.RecordKey}",
                                Timestamp = backlink.SeenAt
                            };
                        }

                        break;

                    default:
                        break;
                }
            }
        }
    }
}
