namespace Pandacap.Models
{
    public record BridgyFedBotAccountMessageModel
    {
        public required bool Incoming { get; init; }
        public required string Content { get; init; }
        public required DateTimeOffset Timestamp { get; init; }
    }
}
