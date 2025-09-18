using Microsoft.EntityFrameworkCore;
using Pandacap.Clients.ATProto;
using Pandacap.ConfigurationObjects;
using Pandacap.Data;
using Pandacap.PlatformBadges;

namespace Pandacap.Notifications
{
    public class ATProtoNotificationHandler(
        DIDResolver didResolver,
        IHttpClientFactory httpClientFactory,
        PandacapDbContext context
    ) : INotificationHandler
    {
        private static string? GetBlueskyAppLink(string postAtUri)
        {
            try
            {
                ATProtoRefUri myPostUri = new(postAtUri);
                return $"https://bsky.app/profile/{myPostUri.Components.DID}/post/{myPostUri.Components.RecordKey}";
            }
            catch (Exception)
            {
                return null;
            }
        }

        private async Task<string?> GetDisplayNameAsync(string did)
        {
            try
            {
                var doc = await didResolver.ResolveAsync(did);
                return doc.Handle;
            }
            catch (Exception)
            {
                return did;
            }
        }

        public async IAsyncEnumerable<Notification> GetNotificationsAsync()
        {
            using var client = httpClientFactory.CreateClient();
            client.DefaultRequestHeaders.UserAgent.ParseAdd(UserAgentInformation.UserAgent);

            await foreach (var backlink in context.ATProtoBackLinks
                .OrderByDescending(link => link.SeenAt)
                .AsAsyncEnumerable())
            {
                switch ((backlink.Collection, backlink.Path))
                {
                    case ("app.bsky.feed.post", ".facets[app.bsky.richtext.facet].features[app.bsky.richtext.facet#mention].did"):
                        yield return new Notification
                        {
                            Platform = new NotificationPlatform(
                                "ATProto",
                                PostPlatformModule.GetBadge(PostPlatform.ATProto),
                                viewAllUrl: null),
                            ActivityName = "Mention",
                            Url = $"https://bsky.app/profile/{backlink.DID}/post/{backlink.RecordKey}",
                            UserName = await GetDisplayNameAsync(backlink.DID),
                            UserUrl = $"https://bsky.app/profile/{backlink.DID}",
                            PostUrl = $"https://bsky.app/profile/{backlink.Target}",
                            Timestamp = backlink.SeenAt
                        };

                        break;

                    case ("app.bsky.graph.follow", ".subject"):
                        yield return new Notification
                        {
                            Platform = new NotificationPlatform(
                                "ATProto",
                                PostPlatformModule.GetBadge(PostPlatform.ATProto),
                                viewAllUrl: null),
                            ActivityName = "Follow",
                            UserName = await GetDisplayNameAsync(backlink.DID),
                            UserUrl = $"https://bsky.app/profile/{backlink.DID}",
                            PostUrl = $"https://bsky.app/profile/{backlink.Target}",
                            Timestamp = backlink.SeenAt
                        };

                        break;

                    case ("app.bsky.feed.post", ".reply.parent.uri"):
                        yield return new Notification
                        {
                            Platform = new NotificationPlatform(
                                "ATProto",
                                PostPlatformModule.GetBadge(PostPlatform.ATProto),
                                viewAllUrl: null),
                            ActivityName = "Reply",
                            Url = $"https://bsky.app/profile/{backlink.DID}/post/{backlink.RecordKey}",
                            UserName = await GetDisplayNameAsync(backlink.DID),
                            UserUrl = $"https://bsky.app/profile/{backlink.DID}",
                            PostUrl = GetBlueskyAppLink(backlink.Target),
                            Timestamp = backlink.SeenAt
                        };

                        break;

                    case ("app.bsky.feed.like", ".subject.uri"):
                        yield return new Notification
                        {
                            Platform = new NotificationPlatform(
                                "ATProto",
                                PostPlatformModule.GetBadge(PostPlatform.ATProto),
                                viewAllUrl: null),
                            ActivityName = "Like",
                            UserName = await GetDisplayNameAsync(backlink.DID),
                            UserUrl = $"https://bsky.app/profile/{backlink.DID}",
                            PostUrl = GetBlueskyAppLink(backlink.Target),
                            Timestamp = backlink.SeenAt
                        };

                        break;

                    case ("app.bsky.feed.repost", ".subject.uri"):
                        yield return new Notification
                        {
                            Platform = new NotificationPlatform(
                                "ATProto",
                                PostPlatformModule.GetBadge(PostPlatform.ATProto),
                                viewAllUrl: null),
                            ActivityName = "Repost",
                            UserName = await GetDisplayNameAsync(backlink.DID),
                            UserUrl = $"https://bsky.app/profile/{backlink.DID}",
                            PostUrl = GetBlueskyAppLink(backlink.Target),
                            Timestamp = backlink.SeenAt
                        };

                        break;

                    default:
                        break;
                }
            }
        }
    }
}
