using Pandacap.PlatformLinks.Interfaces;
using Pandacap.PlatformLinks.Models;

namespace Pandacap.PlatformLinks.LinkTemplates
{
    public record BlueskyLinkTemplate(
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

        public string? GetUrl(PlatformLinkContext context)
        {
            if (context is PlatformLinkContext.Post post
                && post.Item.BlueskyDID is string did
                && post.Item.BlueskyRecordKey is string rkey)
            {
                return $"https://{Host}/profile/{did}/post/{rkey}";
            }

            if (context.IsProfile)
            {
                return $"https://{Host}/profile/{Handle ?? DID}";
            }

            return null;
        }
    }
}
