namespace Pandacap.Notifications
{
    public interface INotificationHandler
    {
        IAsyncEnumerable<Notification> GetNotificationsAsync();
    }
}
