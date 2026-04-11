using Pandacap.ActivityPub.Static;
using Pandacap.PlatformLinks.Interfaces;

namespace Pandacap.PlatformLinks
{
    public record BrowserPubLink(string Host) : IPlatformLink, IActivityPubProfileLink
    {
        public PlatformLinkCategory Category => PlatformLinkCategory.ActivityPub;

        public string Username => $"@{ActivityPubHostInformation.Username}@{ActivityPubHostInformation.ApplicationHostname}";

        public string? PlatformName => "BrowserPub";

        public string? ViewProfileUrl => $"https://{Host}/{Username}";

        public string? IconFilename => "browserpub.svg";

        public string? GetViewPostUrl(IPlatformLinkPostSource post)
        {
            return $"https://{Host}/{post.ActivityPubObjectId}";
        }
    }
}
