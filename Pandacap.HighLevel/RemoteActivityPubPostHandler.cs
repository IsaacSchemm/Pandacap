using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json.Linq;
using Pandacap.Data;
using Pandacap.HighLevel.ActivityPub;

namespace Pandacap.HighLevel
{
    public class RemoteActivityPubPostHandler(PandacapDbContext context)
    {
        private static readonly IEnumerable<JToken> Empty = [];

        public async Task AddRemotePostAsync(
            RemoteActor sendingActor,
            JToken expandedLdJson,
            bool addToInbox = false,
            bool addToFavorites = false)
        {
            var post = expandedLdJson;

            string attributedTo = (post["https://www.w3.org/ns/activitystreams#attributedTo"] ?? Empty).Single()["@id"]!.Value<string>()!;
            if (attributedTo != sendingActor.Id)
                return;

            string id = post["@id"]!.Value<string>()!;
            IEnumerable<string> types = (post["@type"] ?? Empty).Select(token => token.Value<string>()!);

            RemoteActivityPubPost? existingPost = await context.RemoteActivityPubPosts.FirstOrDefaultAsync(item => item.Id == id);

            if (existingPost == null)
            {
                existingPost = new RemoteActivityPubPost
                {
                    Id = id,
                    CreatedBy = sendingActor.Id,
                    DismissedAt = DateTimeOffset.UtcNow
                };
                context.Add(existingPost);
            }
            else
            {
                if (existingPost.CreatedBy != sendingActor.Id)
                    return;
            }

            if (addToInbox)
                existingPost.DismissedAt = null;

            if (addToFavorites)
                existingPost.FavoritedAt = DateTimeOffset.UtcNow;

            existingPost.Username = sendingActor.PreferredUsername ?? sendingActor.Id;
            existingPost.Usericon = sendingActor.IconUrl;

            existingPost.Timestamp = (post["https://www.w3.org/ns/activitystreams#published"] ?? Empty)
                .Select(token => token["@value"]!.Value<DateTime>())
                .FirstOrDefault();

            existingPost.Summary = (post["https://www.w3.org/ns/activitystreams#summary"] ?? Empty)
                .Select(token => token["@value"]!.Value<string>())
                .FirstOrDefault();
            existingPost.Sensitive = (post["https://www.w3.org/ns/activitystreams#summary"] ?? Empty)
                .Select(token => token["@value"]!.Value<bool>())
                .Contains(true);

            existingPost.Name = (post["https://www.w3.org/ns/activitystreams#name"] ?? Empty)
                .Select(token => token["@value"]!.Value<string>())
                .FirstOrDefault();

            existingPost.Content = (post["https://www.w3.org/ns/activitystreams#content"] ?? Empty)
                .Select(token => token["@value"]!.Value<string>())
                .FirstOrDefault();

            existingPost.Attachments.Clear();

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
                        existingPost.Attachments.Add(new()
                        {
                            Name = name,
                            Url = url
                        });
                        break;
                }
            }

            await context.SaveChangesAsync();
        }
    }
}
