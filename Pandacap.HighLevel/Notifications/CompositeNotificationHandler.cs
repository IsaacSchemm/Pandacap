namespace Pandacap.HighLevel.Notifications
{
    public class CompositeNotificationHandler(
        ActivityPubNotificationHandler activityPubNotificationHandler,
        ActivityPubReplyNotificationHandler activityPubNotificationReplyHandler,
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
