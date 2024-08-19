using Pandacap.HighLevel;

namespace Pandacap.Models
{
    public class NotificationsViewModel
    {
        public IEnumerable<ActivityPubNotificationHandler.Notification> RecentActivities { get; set; } = [];
        public IEnumerable<ATProtoNotificationHandler.Notification> RecentATProtoNotifications { get; set; } = [];
    }
}
