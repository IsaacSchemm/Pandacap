using Pandacap.LowLevel;

namespace Pandacap.HighLevel.Notifications
{
    public record Notification
    {
        public required NotificationPlatform Platform { get; init; }
        public required string ActivityName { get; init; }
        public string? UserName { get; init; }
        public string? UserUrl { get; init; }
        public string? PostUrl { get; init; }
        public required DateTimeOffset Timestamp { get; init; }
    }
}
