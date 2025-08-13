using DeviantArtFs.Extensions;
using Microsoft.FSharp.Core;
using Pandacap.Clients.ATProto.Private;
using Pandacap.ConfigurationObjects;
using Pandacap.HighLevel.ATProto;
using Pandacap.PlatformBadges;
using ATProtoNotifications = Pandacap.Clients.ATProto.Private.Notifications;

namespace Pandacap.HighLevel.Notifications
{
    public class ATProtoNotificationHandler(
        ATProtoCredentialProvider atProtoCredentialProvider,
        IHttpClientFactory httpClientFactory
    ) : INotificationHandler
    {
        public async IAsyncEnumerable<Notification> GetNotificationsAsync()
        {
            var allCredentials = await atProtoCredentialProvider.GetAllCredentialsAsync();
            var results = allCredentials
                .Select(GetNotificationsAsync)
                .MergeNewest(post => post.Timestamp);
            await foreach (var result in results)
                yield return result;
        }

        private async IAsyncEnumerable<Notification> GetNotificationsAsync(
            IAutomaticRefreshCredentials credentials)
        {
            var client = httpClientFactory.CreateClient();
            client.DefaultRequestHeaders.UserAgent.ParseAdd(UserAgentInformation.UserAgent);

            var page = Page.FromStart;

            while (true)
            {
                var result = await ATProtoNotifications.ListNotificationsAsync(
                    client,
                    credentials,
                    page);

                foreach (var item in result.notifications)
                {
                    yield return new()
                    {
                        Platform = new NotificationPlatform(
                            "Bluesky",
                            PostPlatformModule.GetBadge(PostPlatform.ATProto),
                            "https://bsky.app/notifications"),
                        ActivityName = item.reason,
                        Url = item.RecordKey != null && item.reason == "reply"
                            ? $"https://bsky.app/profile/{item.author.did}/post/{Uri.EscapeDataString(item.RecordKey)}"
                            : null,
                        UserName = item.author.displayName.OrNull() ?? item.author.handle,
                        UserUrl = $"https://bsky.app/profile/{item.author.did}",
                        PostUrl = item.ReasonSubjectRecordKey != null
                            ? $"https://bsky.app/profile/{credentials.DID}/post/{Uri.EscapeDataString(item.ReasonSubjectRecordKey)}"
                            : null,
                        Timestamp = item.indexedAt.ToUniversalTime()
                    };
                }

                if (OptionModule.ToObj(result.cursor) is string next)
                    page = Page.NewFromCursor(next);
                else
                    yield break;
            }
        }
    }
}
