using Pandacap.Extensions;
using Pandacap.Notifications.Interfaces;
using Pandacap.UI.Badges;

namespace Pandacap
{
    public class CompositeNotificationHandler(
        IEnumerable<INotificationHandler> notificationHandlers
    )
    {
        public IAsyncEnumerable<INotification> GetNotificationsAsync() =>
            notificationHandlers
            .Select(handler => new NotificationFailureHandler(handler))
            .Select(handler => handler.GetNotificationsAsync())
            .MergeNewest(post => post.Timestamp);

        private class NotificationFailureHandler(INotificationHandler underlyingHandler)
        {
            public async IAsyncEnumerable<INotification> GetNotificationsAsync()
            {
                var enumerable = underlyingHandler.GetNotificationsAsync();
                var enumerator = enumerable.GetAsyncEnumerator();

                INotification? initial = null;
                INotification? error = null;

                try
                {
                    if (await enumerator.MoveNextAsync())
                        initial = enumerator.Current;
                }
                catch (Exception ex)
                {
                    error = new ErrorNotification(ex, underlyingHandler);
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

        private record ErrorNotification(
            Exception Exception,
            INotificationHandler UnderlyingHandler) : INotification
        {
            public string? ActivityName => Exception.GetType().Name;
            public Badge Badge => Badges.Pandacap;
            public string? Url => null;
            public string? UserName => $"{UnderlyingHandler.GetType().Name}: {Exception.Message}";
            public string? UserUrl => null;
            public string? PostUrl => null;
            public DateTimeOffset Timestamp => DateTime.UtcNow;
        }
    }
}
