using Pandacap.Notifications.Interfaces;
using Pandacap.UI.Badges;

namespace Pandacap.Notifications
{
    internal record Notification : INotification
    {
        public required string ActivityName { get; init; }
        public required Badge Badge { get; init; }
        public string? Url { get; init; }
        public string? UserName { get; init; }
        public string? UserUrl { get; init; }
        public string? PostUrl { get; init; }
        public required DateTimeOffset Timestamp { get; init; }
    }
}
