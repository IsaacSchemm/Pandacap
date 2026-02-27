using Pandacap.HighLevel;

namespace Pandacap.Notifications
{
    public class CompositeNotificationHandler(
        ActivityPubAddressedPostNotificationHandler activityPubAddressedPostNotificationHandler,
        ActivityPubNotificationHandler activityPubNotificationHandler,
        ActivityPubReplyNotificationHandler activityPubNotificationReplyHandler,
        ATProtoNotificationHandler atProtoNotificationHandler,
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
                //activityPubAddressedPostNotificationHandler,
                //activityPubNotificationHandler,
                //activityPubNotificationReplyHandler,
                //atProtoNotificationHandler,
                //deviantArtFeedNotificationHandler,
                //deviantArtNoteNotificationHandler,
                furAffinityNoteNotificationHandler,
                //furAffinityNotificationHandler,
                //weasylNoteNotificationHandler,
                //weasylNotificationHandler
            }
            //.Select(handler => new NotificationFailureHandler(handler))
            .Select(handler => handler.GetNotificationsAsync())
            .MergeNewest(post => post.Timestamp);
    }
}
