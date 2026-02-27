using Pandacap.ActivityPub;
using Pandacap.ConfigurationObjects;
using Pandacap.Data;

namespace Pandacap.HighLevel.PlatformLinks
{
    public record BrowserPubLink(
        ActivityPubHostInformation ActivityPubHostInformation,
        ApplicationInformation ApplicationInformation,
        string Host) : IPlatformLink
    {
        public PlatformLinkCategory Category => PlatformLinkCategory.ActivityPub;

        public string PlatformName => Host;

        public string Username => $"@{ApplicationInformation.Username}@{ApplicationInformation.ApplicationHostname}";

        public string? IconUrl => $"https://{Host}/icon.svg";

        public string? ViewProfileUrl => $"https://{Host}/{Username}";

        public string? GetViewPostUrl(Post post)
        {
            IActivityPubPost apPost = post;
            return $"https://{Host}/{Uri.EscapeDataString(apPost.GetObjectId(ActivityPubHostInformation))}";
        }
    }
}
