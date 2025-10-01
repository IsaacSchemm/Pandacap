using Pandacap.PlatformBadges;

namespace Pandacap.Notifications
{
    public record Notification
    {
        public required string ActivityName { get; init; }
        public required NotificationPlatform Platform { get; init; }
        public string? Url { get; init; }
        public string? UserName { get; init; }
        public string? UserUrl { get; init; }
        public string? PostUrl { get; init; }
        public required DateTimeOffset Timestamp { get; init; }
    }
}
