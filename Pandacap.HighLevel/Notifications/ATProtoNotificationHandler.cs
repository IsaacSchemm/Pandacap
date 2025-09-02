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

                foreach (var item in result.Items)
                {
                    var rs = item.ReasonSubject;

                    yield return new()
                    {
                        Platform = new NotificationPlatform(
                            "Bluesky",
                            PostPlatformModule.GetBadge(PostPlatform.Bluesky),
                            "https://bsky.app/notifications"),
                        ActivityName = item.Reason,
                        Url = item.Reason == "reply"
                            ? $"https://bsky.app/profile/{item.Actor.DID}/post/{Uri.EscapeDataString(item.Ref.Uri.Components.RecordKey)}"
                            : null,
                        UserName = item.Actor.DisplayName ?? item.Actor.Handle,
                        UserUrl = $"https://bsky.app/profile/{item.Actor.DID}",
                        PostUrl = item.ReasonSubject.Components.Collection == NSIDs.App.Bsky.Feed.Post
                            ? $"https://bsky.app/profile/{item.ReasonSubject.Components.DID}/post/{Uri.EscapeDataString(item.ReasonSubject.Components.Collection)}"
                            : null,
                        Timestamp = item.IndexedAt.ToUniversalTime()
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
