using Pandacap.ActivityPub.Models.Interfaces;
using Pandacap.ConfigurationObjects;
using Pandacap.Data;

namespace Pandacap.HighLevel.PlatformLinks
{
    public record BrowserPubLink(
        ApplicationInformation ApplicationInformation,
        string Host) : IPlatformLink
    {
        public PlatformLinkCategory Category => PlatformLinkCategory.ActivityPub;

        public string Username => ApplicationInformation.ActivityPubWebFingerHandle;

        public string? PlatformName => "BrowserPub";

        public string? ViewProfileUrl => $"https://{Host}/{Username}";

        public string? GetViewPostUrl(Post post)
        {
            IActivityPubPost p = post;
            return $"https://{Host}/{p.ObjectId}";
        }
    }
}
