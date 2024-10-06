using DeviantArtFs.Extensions;
using Microsoft.EntityFrameworkCore;
using Microsoft.FSharp.Core;
using Pandacap.Data;
using Pandacap.LowLevel;

namespace Pandacap.HighLevel.Notifications
{
    public class ATProtoNotificationHandler(
        ApplicationInformation appInfo,
        ATProtoCredentialProvider atProtoCredentialProvider,
        PandacapDbContext context,
        IHttpClientFactory httpClientFactory
    ) : INotificationHandler
    {
        public async IAsyncEnumerable<Notification> GetNotificationsAsync()
        {
            var credentials = await atProtoCredentialProvider.GetCredentialsAsync();
            if (credentials == null)
                yield break;

            var client = httpClientFactory.CreateClient();
            client.DefaultRequestHeaders.UserAgent.ParseAdd(appInfo.UserAgent);

            var page = LowLevel.ATProto.Notifications.Page.FromStart;

            while (true)
            {
                var result = await LowLevel.ATProto.Notifications.ListNotificationsAsync(
                    client,
                    credentials,
                    page);

                foreach (var item in result.notifications)
                {
                    UserPost? userPost = null;

                    if (item.reasonSubject != null)
                    {
                        string rkey = item.reasonSubject.Split('/').Last();
                        userPost = await context.UserPosts
                            .Where(d => d.BlueskyRecordKey == rkey)
                            .FirstOrDefaultAsync();
                    }

                    yield return new()
                    {
                        Platform = NotificationPlatform.ATProto,
                        ActivityName = item.reason,
                        UserName = item.author.displayName.OrNull() ?? item.author.handle,
                        UserUrl = $"https://bsky.app/profile/{item.author.did}",
                        PostUrl = userPost == null
                            ? null
                            : $"https://bsky.app/profile/{userPost.BlueskyDID}/post/{userPost.BlueskyRecordKey}",
                        Timestamp = item.indexedAt.ToUniversalTime()
                    };
                }

                if (OptionModule.ToObj(result.cursor) is string next)
                    page = LowLevel.ATProto.Notifications.Page.NewFromCursor(next);
                else
                    yield break;
            }
        }
    }
}
