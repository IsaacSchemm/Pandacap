using Pandacap.Data;

namespace Pandacap.HighLevel.PlatformLinks
{
    public record BlueskyStyleATProtoPlatformLink(
        string PlatformName,
        string IconUrl,
        string Host,
        string DID,
        string? Handle) : IPlatformLink
    {
        public string Username => Handle != null
            ? $"@{Handle}"
            : DID;

        public string? ViewProfileUrl => $"https://{Host}/profile/{Uri.EscapeDataString(Handle ?? DID)}";

        public PlatformLinkCategory Category => PlatformLinkCategory.ATProto;

        public string? GetViewPostUrl(Post post)
        {
            if (post.BlueskyDID == null || post.BlueskyRecordKey == null)
                return null;

            string handleToUse = post.BlueskyDID == DID
                ? Handle ?? DID
                : post.BlueskyDID;

            return $"https://{Host}/profile/{Uri.EscapeDataString(handleToUse)}/post/{Uri.EscapeDataString(post.BlueskyRecordKey)}";
        }
    }
}
