using Pandacap.Database;

namespace Pandacap.HighLevel.PlatformLinks
{
    public record BlueskyStyleATProtoPlatformLink(
        string PlatformName,
        string Host,
        string DID,
        string? Handle) : IPlatformLink
    {
        public string Username => Handle != null
            ? $"@{Handle}"
            : DID;

        public string? ViewProfileUrl => $"https://{Host}/profile/{Handle ?? DID}";

        public PlatformLinkCategory Category => PlatformLinkCategory.ATProto;

        public string? GetViewPostUrl(Post post) =>
            post.BlueskyDID == null || post.BlueskyRecordKey == null
            ? null
            : $"https://{Host}/profile/{post.BlueskyDID}/post/{post.BlueskyRecordKey}";
    }
}
