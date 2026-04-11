using Pandacap.ActivityPub.Static;
using Pandacap.PlatformLinks.Interfaces;

namespace Pandacap.PlatformLinks.LinkTemplates
{
    public record BrowserPubLinkTemplate(string Host) : ILinkTemplate
    {
        public PlatformLinkCategory Category => PlatformLinkCategory.ActivityPub;

        public string? IconFilename => "browserpub.svg";

        public string? PlatformName => "BrowserPub";

        public string Username => $"@{ActivityPubHostInformation.Username}@{ActivityPubHostInformation.ApplicationHostname}";

        public string? GetViewPostUrl(IPlatformLinkPostSource post) =>
            $"https://{Host}/{post.ActivityPubObjectId}";

        public string? GetViewProfileUrl() =>
            $"https://{Host}/{Username}";
    }
}
