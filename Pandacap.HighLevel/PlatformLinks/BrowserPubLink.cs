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

        public string Username => ApplicationInformation.ActivityPubWebFingerHandle;

        public string? IconUrl => null;

        public string? ViewProfileUrl => $"https://{Host}/{Username}";

        public string? GetViewPostUrl(Post post)
        {
            IActivityPubPost p = post;
            return $"https://{Host}/{p.GetObjectId(ActivityPubHostInformation)}";
        }
    }
}
