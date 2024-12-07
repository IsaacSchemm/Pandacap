namespace Pandacap.Models
{
    public record BridgedPostViewModel
    {
        public required string ActivityPubId { get; init; }
        public required string BlueskyAppUrl { get; init; }
        public required bool Found { get; init; }
        public required string OriginalUrl { get; init; }
        public required string Text { get; init; }
        public DateTimeOffset Timestamp { get; init; }
    }
}
