using Microsoft.EntityFrameworkCore;
using Microsoft.FSharp.Core;
using Pandacap.Data;
using Pandacap.LowLevel;
using Pandacap.LowLevel.ATProto;

namespace Pandacap.HighLevel
{
    public class ATProtoNotificationHandler(
        ApplicationInformation appInfo,
        ATProtoCredentialProvider atProtoCredentialProvider,
        PandacapDbContext context,
        IHttpClientFactory httpClientFactory)
    {
        public record Notification(
            string AuthorDisplayName,
            string AuthorUri,
            string Reason,
            DateTimeOffset IndexedAt,
            bool IsRead,
            Guid? UserPostId,
            string? UserPostTitle);

        public async IAsyncEnumerable<Notification> GetNotificationsAsync()
        {
            var credentials = await atProtoCredentialProvider.GetCredentialsAsync();
            if (credentials == null)
                yield break;

            var client = httpClientFactory.CreateClient();
            client.DefaultRequestHeaders.UserAgent.ParseAdd(appInfo.UserAgent);

            var page = Notifications.Page.FromStart;

            while (true)
            {
                var result = await Notifications.ListNotificationsAsync(
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

                    yield return new Notification(
                        OptionModule.ToObj(item.author.displayName) ?? item.author.handle,
                        $"https://bsky.app/profile/{item.author.handle}",
                        item.reason,
                        item.indexedAt,
                        item.isRead,
                        userPost?.Id,
                        userPost?.Title);
                }

                if (OptionModule.ToObj(result.cursor) is string next)
                    page = Notifications.Page.NewFromCursor(next);
                else
                    yield break;
            }
        }
    }
}
