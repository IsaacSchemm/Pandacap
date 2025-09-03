namespace Pandacap.Models
{
    public record ATProtoFeedModel
    {
        public required string DID { get; init; }

        public string? Handle { get; init; }
        public string? Avatar { get; init; }

        public bool IncludePostsWithoutImages { get; init; }
        public bool IncludeReplies { get; init; }
        public bool IncludeQuotePosts { get; init; }

        public bool IgnoreImages { get; init; }

        public bool IncludeBlueskyLikes { get; init; }
        public bool IncludeBlueskyPosts { get; init; }
        public bool IncludeBlueskyReposts { get; init; }
        public bool IncludeWhiteWindBlogEntries { get; init; }
    }
}
