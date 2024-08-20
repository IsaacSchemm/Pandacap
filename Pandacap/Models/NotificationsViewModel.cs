using Pandacap.Data;
using Pandacap.HighLevel;

namespace Pandacap.Models
{
    public class NotificationsViewModel
    {
        public IEnumerable<ActivityPubNotificationHandler.Notification> RecentActivityPubActivities { get; set; } = [];
        public IEnumerable<InboxActivityStreamsPost> RecentActivityPubPosts { get; set; } = [];
        public IEnumerable<ATProtoNotificationHandler.Notification> RecentATProtoNotifications { get; set; } = [];
        public IEnumerable<DeviantArtNotificationsHandler.Message> RecentDeviantArtMessages { get; set; } = [];
    }
}
