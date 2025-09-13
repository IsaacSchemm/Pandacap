using Pandacap.PlatformBadges;

namespace Pandacap.HighLevel.Notifications
{
    public class NotificationFailureHandler(INotificationHandler underlyingHandler) : INotificationHandler
    {
        public async IAsyncEnumerable<Notification> GetNotificationsAsync()
        {
            var enumerable = underlyingHandler.GetNotificationsAsync();
            var enumerator = enumerable.GetAsyncEnumerator();

            Notification? initial = null;
            Notification? error = null;

            try
            {
                if (await enumerator.MoveNextAsync())
                    initial = enumerator.Current;
            }
            catch (Exception ex)
            {
                error = new Notification
                {
                    ActivityName = ex.GetType().Name,
                    Platform = new NotificationPlatform(
                        "Pandacap",
                        PostPlatformModule.GetBadge(PostPlatform.Pandacap),
                        null),
                    Timestamp = DateTime.UtcNow,
                    UserName = underlyingHandler.GetType().Name + ": " + ex.Message
                };
            }

            if (error != null)
            {
                yield return error;
                yield break;
            }

            if (initial != null)
                yield return initial;

            while (await enumerator.MoveNextAsync())
                yield return enumerator.Current;
        }
    }
}
