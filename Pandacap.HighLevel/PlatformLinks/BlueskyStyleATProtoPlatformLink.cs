using Pandacap.Data;

namespace Pandacap.HighLevel.PlatformLinks
{
    public record BlueskyStyleATProtoPlatformLink(
        string Host,
        string DID,
        string? Handle) : IPlatformLink
    {
        public string Username => Handle != null
            ? $"@{Handle}"
            : DID;

        public string? ViewProfileUrl => $"https://{Host}/profile/{Uri.EscapeDataString(Handle ?? DID)}";

        public PlatformLinkCategory Category => PlatformLinkCategory.ATProto;

        public string? IconUrl => null;

        public string? GetViewPostUrl(Post post) =>
            post.BlueskyDID == null || post.BlueskyRecordKey == null
            ? null
            : $"https://{Host}/profile/{Uri.EscapeDataString(post.BlueskyDID)}/post/{Uri.EscapeDataString(post.BlueskyRecordKey)}";
    }
}
