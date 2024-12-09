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

        public bool SpecialBehavior =>
            ExcludeImageShares
            || ExcludeTextShares
            || ExcludeQuotePosts;
    }
}
