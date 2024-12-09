namespace Pandacap.Models
{
    public record BlueskyFollowModel
    {
        public required string DID { get; init; }
        public string? Handle { get; init; }
        public string? Avatar { get; init; }

        public bool ExcludeImageShares { get; init; }
        public bool ExcludeTextShares { get; init; }
        public bool ExcludeQuotePosts { get; init; }

        public IEnumerable<string> SpecialBehaviorDescriptions
        {
            get
            {
                if (ExcludeImageShares)
                    yield return "Exclude image reposts";
                if (ExcludeTextShares)
                    yield return "Exclude text reposts";
                if (ExcludeQuotePosts)
                    yield return "Exclude quote posts";
            }
        }
    }
}
