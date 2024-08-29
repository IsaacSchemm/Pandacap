using Pandacap.LowLevel;

namespace Pandacap.HighLevel.Notifications
{
    public class Notification
    {
        public required NotificationPlatform Platform { get; init; }
        public required string ActivityName { get; init; }
        public string? UserName { get; init; }
        public string? UserUrl { get; init; }
        public Guid? UserPostId { get; init; }
        public string? UserPostTitle { get; init; }
        public required DateTimeOffset Timestamp { get; init; }
    }
}
