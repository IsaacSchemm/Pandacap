using JsonLD.Core;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json.Linq;
using Pandacap.Data;
using Pandacap.HighLevel.ActivityPub;
using Pandacap.LowLevel;

namespace Pandacap.HighLevel
{
    public class RemoteActivityPubPostHandler(
        PandacapDbContext context,
        RemoteActorFetcher remoteActorFetcher,
        ActivityPubTranslator translator)
    {
        private static readonly IEnumerable<JToken> Empty = [];

        private static IEnumerable<(string? name, string url)> GetAttachments(JToken post)
        {
            foreach (var attachment in post["https://www.w3.org/ns/activitystreams#attachment"] ?? Empty)
            {
                string? mediaType = (attachment["https://www.w3.org/ns/activitystreams#mediaType"] ?? Empty)
                    .Select(token => token["@value"]!.Value<string>())
                    .FirstOrDefault();
                string? url = (attachment["https://www.w3.org/ns/activitystreams#url"] ?? Empty)
                    .Select(token => token["@id"]!.Value<string>())
                    .FirstOrDefault();
                string? name = (attachment["https://www.w3.org/ns/activitystreams#name"] ?? Empty)
                    .Select(token => token["@value"]!.Value<string>())
                    .FirstOrDefault();

                if (url == null)
                    continue;

                switch (mediaType)
                {
                    case "image/jpeg":
                    case "image/png":
                    case "image/gif":
                    case "image/webp":
                    case "image/heif":
                        yield return (name, url);
                        break;
                }
            }
        }

        public async Task AddRemotePostAsync(
            RemoteActor sendingActor,
            JToken expandedLdJson,
            bool isMention = false,
            bool isReply = false)
        {
            var post = expandedLdJson;

            string attributedTo = (post["https://www.w3.org/ns/activitystreams#attributedTo"] ?? Empty).Single()["@id"]!.Value<string>()!;
            if (attributedTo != sendingActor.Id)
                return;

            string id = post["@id"]!.Value<string>()!;
            IEnumerable<string> types = (post["@type"] ?? Empty).Select(token => token.Value<string>()!);

            int existing = await context.RemoteActivityPubPosts.Where(p => p.Id == id).CountAsync();
            if (existing > 0)
                return;

            context.Add(new RemoteActivityPubPost
            {
                Id = id,
                CreatedBy = sendingActor.Id,
                IsMention = isMention,
                IsReply = isReply,
                Username = sendingActor.PreferredUsername,
                Usericon = sendingActor.IconUrl,
                Timestamp = (post["https://www.w3.org/ns/activitystreams#published"] ?? Empty)
                    .Select(token => token["@value"]!.Value<DateTime>())
                    .FirstOrDefault(),
                Summary = (post["https://www.w3.org/ns/activitystreams#summary"] ?? Empty)
                    .Select(token => token["@value"]!.Value<string>())
                    .FirstOrDefault(),
                Sensitive = (post["https://www.w3.org/ns/activitystreams#summary"] ?? Empty)
                    .Select(token => token["@value"]!.Value<bool>())
                    .Contains(true),
                Name = (post["https://www.w3.org/ns/activitystreams#name"] ?? Empty)
                    .Select(token => token["@value"]!.Value<string>())
                    .FirstOrDefault(),
                Content = (post["https://www.w3.org/ns/activitystreams#content"] ?? Empty)
                    .Select(token => token["@value"]!.Value<string>())
                    .FirstOrDefault(),
                Attachments = GetAttachments(post)
                    .Select(attachment => new RemoteActivityPubPost.ImageAttachment
                    {
                        Name = attachment.name,
                        Url = attachment.url
                    })
                    .ToList()
            });
            await context.SaveChangesAsync();
        }

        public async Task AddRemoteAnnouncementAsync(
            RemoteActor announcingActor,
            string announceActivityId,
            string objectId)
        {
            string originalPostJson = await remoteActorFetcher.GetJsonAsync(new Uri(objectId));
            JToken originalPost = JsonLdProcessor.Expand(JObject.Parse(originalPostJson))[0];

            if (!GetAttachments(originalPost).Any())
                return;

            string? originalActorId = (originalPost["https://www.w3.org/ns/activitystreams#attributedTo"] ?? Empty)
                .Select(token => token["@id"]!.Value<string>())
                .FirstOrDefault();
            if (originalActorId == null)
                return;

            var originalActor = await remoteActorFetcher.FetchActorAsync(originalActorId);

            var olderAnnouncements = context.RemoteActivityPubAnnouncements
                .Where(a => a.SharedBy.Id == announcingActor.Id)
                .OrderByDescending(a => a.SharedAt)
                .Skip(11)
                .AsAsyncEnumerable();

            await foreach (var olderAnnouncement in olderAnnouncements)
                context.Remove(olderAnnouncement);

            context.Add(new RemoteActivityPubAnnouncement
            {
                AnnounceActivityId = announceActivityId,
                ObjectId = objectId,
                CreatedBy = new()
                {
                    Id = originalActor.Id,
                    Username = originalActor.PreferredUsername,
                    Usericon = originalActor.IconUrl
                },
                SharedBy = new()
                {
                    Id = announcingActor.Id,
                    Username = announcingActor.PreferredUsername,
                    Usericon = announcingActor.IconUrl
                },
                SharedAt = DateTimeOffset.UtcNow,
                Summary = (originalPost["https://www.w3.org/ns/activitystreams#summary"] ?? Empty)
                    .Select(token => token["@value"]!.Value<string>())
                    .FirstOrDefault(),
                Sensitive = (originalPost["https://www.w3.org/ns/activitystreams#summary"] ?? Empty)
                    .Select(token => token["@value"]!.Value<bool>())
                    .Contains(true),
                Name = (originalPost["https://www.w3.org/ns/activitystreams#name"] ?? Empty)
                    .Select(token => token["@value"]!.Value<string>())
                    .FirstOrDefault(),
                Content = (originalPost["https://www.w3.org/ns/activitystreams#content"] ?? Empty)
                    .Select(token => token["@value"]!.Value<string>())
                    .FirstOrDefault(),
                Attachments = GetAttachments(originalPost)
                    .Select(attachment => new RemoteActivityPubAnnouncement.ImageAttachment
                    {
                        Name = attachment.name,
                        Url = attachment.url
                    })
                    .ToList()
            });

            await context.SaveChangesAsync();
        }

        public async Task AddRemoteFavoriteAsync(
            string objectId)
        {
            string postJson = await remoteActorFetcher.GetJsonAsync(new Uri(objectId));
            JToken post = JsonLdProcessor.Expand(JObject.Parse(postJson))[0];

            string? originalActorId = (post["https://www.w3.org/ns/activitystreams#attributedTo"] ?? Empty)
                .Select(token => token["@id"]!.Value<string>())
                .FirstOrDefault();
            if (originalActorId == null)
                return;

            var originalActor = await remoteActorFetcher.FetchActorAsync(originalActorId);

            Guid likeGuid = Guid.NewGuid();

            context.Add(new RemoteActivityPubFavorite
            {
                LikeGuid = likeGuid,
                ObjectId = objectId,
                CreatedBy = originalActor.Id,
                Username = originalActor.PreferredUsername,
                Usericon = originalActor.IconUrl,
                CreatedAt = (post["https://www.w3.org/ns/activitystreams#published"] ?? Empty)
                    .Select(token => token["@value"]!.Value<DateTime>())
                    .FirstOrDefault(),
                FavoritedAt = DateTimeOffset.UtcNow,
                Summary = (post["https://www.w3.org/ns/activitystreams#summary"] ?? Empty)
                    .Select(token => token["@value"]!.Value<string>())
                    .FirstOrDefault(),
                Sensitive = (post["https://www.w3.org/ns/activitystreams#summary"] ?? Empty)
                    .Select(token => token["@value"]!.Value<bool>())
                    .Contains(true),
                Name = (post["https://www.w3.org/ns/activitystreams#name"] ?? Empty)
                    .Select(token => token["@value"]!.Value<string>())
                    .FirstOrDefault(),
                Content = (post["https://www.w3.org/ns/activitystreams#content"] ?? Empty)
                    .Select(token => token["@value"]!.Value<string>())
                    .FirstOrDefault(),
                Attachments = GetAttachments(post)
                    .Select(attachment => new RemoteActivityPubFavorite.ImageAttachment
                    {
                        Name = attachment.name,
                        Url = attachment.url
                    })
                    .ToList()
            });
            context.ActivityPubOutboundActivities.Add(new()
            {
                Id = Guid.NewGuid(),
                Inbox = originalActor.Inbox,
                JsonBody = ActivityPubSerializer.SerializeWithContext(translator.Like(likeGuid, objectId))
            });
            await context.SaveChangesAsync();
        }
    }
}
