using Microsoft.FSharp.Collections;

namespace Pandacap.Models
{
    public record BridgyFedViewModel
    {
        public required FSharpList<BridgedPost> BridgedPosts { get; init; }
        public required int Offset { get; init; }
        public required int Count { get; init; }

        public record BridgedPost
        {
            public required string ActivityPubId { get; init; }
            public required string BlueskyAppUrl { get; init; }
            public required bool Found { get; init; }
            public required string? Handle { get; init; }
            public required string OriginalUrl { get; init; }
            public required string Text { get; init; }
            public DateTimeOffset Timestamp { get; init; }
        }
    }
}
