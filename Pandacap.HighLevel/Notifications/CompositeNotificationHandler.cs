namespace Pandacap.HighLevel.Notifications
{
    public class CompositeNotificationHandler(
        ActivityPubNotificationHandler activityPubNotificationHandler,
        ActivityPubReplyNotificationHandler activityPubNotificationReplyHandler,
        ATProtoNotificationHandler atProtoNotificationHandler,
        DeviantArtFeedNotificationHandler deviantArtFeedNotificationHandler,
        DeviantArtNoteNotificationHandler deviantArtNoteNotificationHandler,
        WeasylNotificationHandler weasylNotificationHandler
    ) : INotificationHandler
    {
        public IAsyncEnumerable<Notification> GetNotificationsAsync() =>
            new INotificationHandler[]
            {
                activityPubNotificationHandler,
                activityPubNotificationReplyHandler,
                atProtoNotificationHandler,
                deviantArtFeedNotificationHandler,
                deviantArtNoteNotificationHandler,
                weasylNotificationHandler
            }
            .Select(handler => handler.GetNotificationsAsync())
            .MergeNewest(post => post.Timestamp);
    }
}
