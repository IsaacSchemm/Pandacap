using Pandacap.ActivityPub.Static;
using Pandacap.PlatformLinks.Interfaces;
using Pandacap.PlatformLinks.Models;

namespace Pandacap.PlatformLinks.LinkTemplates
{
    public record BrowserPubLinkTemplate(string Host) : ILinkTemplate
    {
        public PlatformLinkCategory Category => PlatformLinkCategory.ActivityPub;

        public string? IconFilename => "browserpub.svg";

        public string? PlatformName => "BrowserPub";

        public string Username => $"@{ActivityPubHostInformation.Username}@{ActivityPubHostInformation.ApplicationHostname}";

        public string? GetUrl(PlatformLinkContext context) =>
            context is PlatformLinkContext.Post post ? $"https://{Host}/{post.Item.ActivityPubObjectId}"
            : context.IsProfile ? $"https://{Host}/{Username}"
            : null;
    }
}
