using Microsoft.FSharp.Core;
using Pandacap.LowLevel;
using Pandacap.LowLevel.ATProto;

namespace Pandacap.HighLevel
{
    public class ATProtoNotificationHandler(
        ApplicationInformation appInfo,
        ATProtoCredentialProvider atProtoCredentialProvider,
        IHttpClientFactory httpClientFactory)
    {
        public async IAsyncEnumerable<Notifications.Notification> GetNotificationsAsync()
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
                    yield return item;

                if (OptionModule.ToObj(result.cursor) is string next)
                    page = Notifications.Page.NewFromCursor(next);
                else
                    yield break;
            }
        }
    }
}
