using Pandacap.HighLevel;

namespace Pandacap.Notifications
{
    public class CompositeNotificationHandler(
        ActivityPubNotificationHandler activityPubNotificationHandler,
        ActivityPubReplyNotificationHandler activityPubNotificationReplyHandler,
        ATProtoNotificationHandler atProtoNotificationHandler,
        BlueskyNotificationHandler blueskyNotificationHandler,
        DeviantArtFeedNotificationHandler deviantArtFeedNotificationHandler,
        DeviantArtNoteNotificationHandler deviantArtNoteNotificationHandler,
        FurAffinityNoteNotificationHandler furAffinityNoteNotificationHandler,
        FurAffinityNotificationHandler furAffinityNotificationHandler,
        WeasylNoteNotificationHandler weasylNoteNotificationHandler,
        WeasylNotificationHandler weasylNotificationHandler
    ) : INotificationHandler
    {
        public IAsyncEnumerable<Notification> GetNotificationsAsync() =>
            new INotificationHandler[]
            {
                activityPubNotificationHandler,
                activityPubNotificationReplyHandler,
                atProtoNotificationHandler,
                blueskyNotificationHandler,
                deviantArtFeedNotificationHandler,
                deviantArtNoteNotificationHandler,
                furAffinityNoteNotificationHandler,
                furAffinityNotificationHandler,
                weasylNoteNotificationHandler,
                weasylNotificationHandler
            }
            .Select(handler => new NotificationFailureHandler(handler))
            .Select(handler => handler.GetNotificationsAsync())
            .MergeNewest(post => post.Timestamp);
    }
}
