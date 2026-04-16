using Pandacap.PlatformLinks.Interfaces;

namespace Pandacap.PlatformLinks.LinkTemplates
{
    public record BlueskyStyleATProtoPlatformLinkTemplate(
        string PlatformName,
        string IconFilename,
        string Host,
        string DID,
        string? Handle) : ILinkTemplate
    {
        public PlatformLinkCategory Category => PlatformLinkCategory.Bluesky;

        public string Username =>
            Handle != null
            ? $"@{Handle}"
            : DID;

        public string? GetViewPostUrl(IPlatformLinkPostSource post) =>
            post.BlueskyDID == null || post.BlueskyRecordKey == null
            ? null
            : $"https://{Host}/profile/{post.BlueskyDID}/post/{post.BlueskyRecordKey}";

        public string? GetViewProfileUrl() =>
            $"https://{Host}/profile/{Handle ?? DID}";
    }
}
