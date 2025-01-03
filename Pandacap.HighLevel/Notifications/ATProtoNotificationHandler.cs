﻿using DeviantArtFs.Extensions;
using Microsoft.FSharp.Core;
using Pandacap.LowLevel;
using Pandacap.Types;

namespace Pandacap.HighLevel.Notifications
{
    public class ATProtoNotificationHandler(
        ATProtoCredentialProvider atProtoCredentialProvider,
        IHttpClientFactory httpClientFactory
    ) : INotificationHandler
    {
        public async IAsyncEnumerable<Notification> GetNotificationsAsync()
        {
            var credentials = await atProtoCredentialProvider.GetCredentialsAsync();
            if (credentials == null)
                yield break;

            var client = httpClientFactory.CreateClient();
            client.DefaultRequestHeaders.UserAgent.ParseAdd(UserAgentInformation.UserAgent);

            var page = LowLevel.ATProto.Page.FromStart;

            while (true)
            {
                var result = await LowLevel.ATProto.Notifications.ListNotificationsAsync(
                    client,
                    credentials,
                    page);

                foreach (var item in result.notifications)
                {
                    string? originalPostRecordKey = (item.reasonSubject ?? "")
                        .Split('/', StringSplitOptions.RemoveEmptyEntries)
                        .LastOrDefault();
                    string? replyRecordKey = (item.uri ?? "")
                        .Split('/', StringSplitOptions.RemoveEmptyEntries)
                        .LastOrDefault();

                    yield return new()
                    {
                        Platform = new NotificationPlatform(
                            "Bluesky",
                            PostPlatformModule.GetBadge(PostPlatform.ATProto),
                            "https://bsky.app/notifications"),
                        ActivityName = item.reason,
                        Url = replyRecordKey != null && item.reason == "reply"
                            ? $"https://bsky.app/profile/{item.author.did}/post/{replyRecordKey}"
                            : null,
                        UserName = item.author.displayName.OrNull() ?? item.author.handle,
                        UserUrl = $"https://bsky.app/profile/{item.author.did}",
                        PostUrl = originalPostRecordKey != null
                            ? $"https://bsky.app/profile/{credentials.DID}/post/{originalPostRecordKey}"
                            : null,
                        Timestamp = item.indexedAt.ToUniversalTime()
                    };
                }

                if (OptionModule.ToObj(result.cursor) is string next)
                    page = LowLevel.ATProto.Page.NewFromCursor(next);
                else
                    yield break;
            }
        }
    }
}
