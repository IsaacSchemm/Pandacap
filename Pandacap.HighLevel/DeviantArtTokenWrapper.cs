using DeviantArtFs;
using Microsoft.EntityFrameworkCore;
using Pandacap.Data;
using Pandacap.HighLevel.ActivityPub;
using Pandacap.LowLevel;

namespace Pandacap.HighLevel
{
    public class DeviantArtTokenWrapper(
        DeviantArtApp app,
        PandacapDbContext context,
        DeviantArtCredentials credentials,
        KeyProvider keyProvider,
        ActivityPubTranslator translator) : IDeviantArtRefreshableAccessToken
    {
        public string RefreshToken => credentials.RefreshToken;
        public string AccessToken => credentials.AccessToken;

        public async Task RefreshAccessTokenAsync()
        {
            var resp = await DeviantArtAuth.RefreshAsync(app, credentials.RefreshToken);
            credentials.RefreshToken = resp.refresh_token;
            credentials.AccessToken = resp.access_token;
            await context.SaveChangesAsync();
        }

        public async Task UpdateUserIconAsync(string? usericon)
        {
            if (credentials.UserIcon != usericon)
            {
                credentials.UserIcon = usericon;

                var key = await keyProvider.GetPublicKeyAsync();

                var followers = await context.Followers
                    .Select(follower => new
                    {
                        follower.Inbox,
                        follower.SharedInbox
                    })
                    .ToListAsync();

                var inboxes = followers
                    .Select(follower => follower.SharedInbox ?? follower.Inbox)
                    .Distinct();

                string activityJson = ActivityPubSerializer.SerializeWithContext(
                    translator.PersonToUpdate(
                        key,
                        usericon));

                foreach (string inbox in inboxes)
                {
                    Guid activityGuid = Guid.NewGuid();

                    context.ActivityPubOutboundActivities.Add(new()
                    {
                        Id = activityGuid,
                        JsonBody = activityJson,
                        Inbox = inbox,
                        StoredAt = DateTimeOffset.UtcNow
                    });
                }

                await context.SaveChangesAsync();
            }
        }
    }
}
