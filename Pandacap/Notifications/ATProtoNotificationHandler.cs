using Microsoft.EntityFrameworkCore;
using Pandacap.Clients.ATProto;
using Pandacap.Data;
using Pandacap.HighLevel;
using Pandacap.PlatformBadges;

namespace Pandacap.Notifications
{
    public class ATProtoNotificationHandler(
        ConstellationClient constellationClient,
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

        private async IAsyncEnumerable<Notification> GetNotificationsAsync(
            string target,
            string collection,
            string path)
        {
            DateTimeOffset seenAt = DateTimeOffset.UtcNow;

            await foreach (var link in constellationClient.ListLinksAsync(
                target,
                collection,
                path,
                CancellationToken.None))
            {
                try
                {
                    using var httpClient = httpClientFactory.CreateClient();

                    var doc = await didResolver.ResolveAsync(link.Components.DID);

                    var record = await XRPC.Com.Atproto.Repo.GetRecordAsync(
                        httpClient,
                        doc.PDS,
                        link.Components.DID,
                        collection,
                        link.Components.RecordKey,
                        new { createdAt = (DateTimeOffset?)null });

                    if (record.value.createdAt is DateTimeOffset createdAt)
                        seenAt = createdAt;
                }
                catch (Exception)
                {
                    continue;
                }

                switch ((collection, path))
                {
                    case ("app.bsky.feed.post", ".facets[app.bsky.richtext.facet].features[app.bsky.richtext.facet#mention].did"):
                        yield return new Notification
                        {
                            Platform = new NotificationPlatform(
                                "ATProto",
                                PostPlatformModule.GetBadge(PostPlatform.ATProto),
                                viewAllUrl: null),
                            ActivityName = "Mention",
                            Url = $"https://bsky.app/profile/{link.Components.DID}/post/{link.Components.RecordKey}",
                            UserName = await GetDisplayNameAsync(link.Components.DID),
                            UserUrl = $"https://bsky.app/profile/{link.Components.DID}",
                            PostUrl = $"https://bsky.app/profile/{target}",
                            Timestamp = seenAt
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
                            UserName = await GetDisplayNameAsync(link.Components.DID),
                            UserUrl = $"https://bsky.app/profile/{link.Components.DID}",
                            PostUrl = $"https://bsky.app/profile/{target}",
                            Timestamp = seenAt
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
                            Url = $"https://bsky.app/profile/{link.Components.DID}/post/{link.Components.RecordKey}",
                            UserName = await GetDisplayNameAsync(link.Components.DID),
                            UserUrl = $"https://bsky.app/profile/{link.Components.DID}",
                            PostUrl = GetBlueskyAppLink(target),
                            Timestamp = seenAt
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
                            UserName = await GetDisplayNameAsync(link.Components.DID),
                            UserUrl = $"https://bsky.app/profile/{link.Components.DID}",
                            PostUrl = GetBlueskyAppLink(target),
                            Timestamp = seenAt
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
                            UserName = await GetDisplayNameAsync(link.Components.DID),
                            UserUrl = $"https://bsky.app/profile/{link.Components.DID}",
                            PostUrl = GetBlueskyAppLink(target),
                            Timestamp = seenAt
                        };

                        break;

                    default:
                        break;
                }
            }
        }

        public async IAsyncEnumerable<Notification> GetNotificationsAsync()
        {
            List<Notification> notifications = [];

            var did = await context.Posts
                .OrderByDescending(post => post.PublishedTime)
                .Where(post => post.BlueskyDID != null)
                .Select(post => post.BlueskyDID)
                .FirstOrDefaultAsync();

            if (did != null)
            {
                notifications.AddRange(
                    await GetNotificationsAsync(
                        did,
                        "app.bsky.feed.post",
                        ".facets[app.bsky.richtext.facet].features[app.bsky.richtext.facet#mention].did")
                    .Take(20)
                    .ToListAsync());

                notifications.AddRange(
                    await GetNotificationsAsync(
                        did,
                        "app.bsky.graph.follow",
                        ".subject")
                    .Take(20)
                    .ToListAsync());
            }

            var cutoff = DateTimeOffset.UtcNow - TimeSpan.FromDays(30);

            var posts = await context.Posts
                .Where(p => p.PublishedTime >= cutoff)
                .Where(p => p.BlueskyDID != null)
                .Where(p => p.BlueskyRecordKey != null)
                .Select(p => new
                {
                    p.BlueskyDID,
                    p.BlueskyRecordKey
                })
                .ToListAsync();

            var addressedPosts = await context.AddressedPosts
                .Where(p => p.PublishedTime >= cutoff)
                .Where(p => p.BlueskyDID != null)
                .Where(p => p.BlueskyRecordKey != null)
                .Select(p => new
                {
                    p.BlueskyDID,
                    p.BlueskyRecordKey
                })
                .ToListAsync();

            var allPosts = posts.Concat(addressedPosts);

            foreach (var p in allPosts)
            {
                var uri = $"at://{p.BlueskyDID}/app.bsky.feed.post/{p.BlueskyRecordKey}";

                var replies = await GetNotificationsAsync(
                    uri,
                    "app.bsky.feed.post",
                    ".reply.parent.uri").ToListAsync();

                var likes = await GetNotificationsAsync(
                    uri,
                    "app.bsky.feed.like",
                    ".subject.uri").ToListAsync();

                var reposts = await GetNotificationsAsync(
                    uri,
                    "app.bsky.feed.repost",
                    ".subject.uri").ToListAsync();

                IEnumerable<Notification> all = [.. replies, .. likes, .. reposts];

                notifications.AddRange(all);
            }

            foreach (var notification in notifications.OrderByDescending(n => n.Timestamp))
                yield return notification;
        }
    }
}
