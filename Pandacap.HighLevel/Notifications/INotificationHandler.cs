namespace Pandacap.HighLevel.Notifications
{
    public interface INotificationHandler
    {
        IAsyncEnumerable<Notification> GetNotificationsAsync();
    }
}
