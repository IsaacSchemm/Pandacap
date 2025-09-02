using DeviantArtFs.Extensions;
using Microsoft.FSharp.Core;
using Pandacap.Clients;
using Pandacap.Clients.ATProto;
using Pandacap.ConfigurationObjects;
using Pandacap.HighLevel.ATProto;
using Pandacap.PlatformBadges;

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
                .Select(GetNotificationsByUserAsync)
                .MergeNewest(post => post.Timestamp);
            await foreach (var result in results)
                yield return result;
        }

        private async IAsyncEnumerable<Notification> GetNotificationsByUserAsync(
           XRPC.ICredentials credentials)
        {
            var client = httpClientFactory.CreateClient();
            client.DefaultRequestHeaders.UserAgent.ParseAdd(UserAgentInformation.UserAgent);

            string? cursor = null;

            while (true)
            {
                var result = await XRPC.App.Bsky.Notification.ListNotificationsAsync(
                    client,
                    credentials,
                    cursor);

                foreach (var item in result.notifications)
                {
                    var rs = item.ReasonSubject;

                    yield return new()
                    {
                        Platform = new NotificationPlatform(
                            "Bluesky",
                            PostPlatformModule.GetBadge(PostPlatform.Bluesky),
                            "https://bsky.app/notifications"),
                        ActivityName = item.reason,
                        Url = item.reason == "reply"
                            ? $"https://bsky.app/profile/{item.author.did}/post/{Uri.EscapeDataString(item.RecordKey)}"
                            : null,
                        UserName = item.author.DisplayName ?? item.author.handle,
                        UserUrl = $"https://bsky.app/profile/{item.author.did}",
                        PostUrl = item.ReasonSubject.Collection == NSIDs.App.Bsky.Feed.Post
                            ? $"https://bsky.app/profile/{credentials.DID}/post/{Uri.EscapeDataString(item.ReasonSubject.RecordKey)}"
                            : null,
                        Timestamp = item.indexedAt.ToUniversalTime()
                    };
                }

                if (result.Cursor is string next)
                    cursor = next;
                else
                    yield break;
            }
        }
    }
}
