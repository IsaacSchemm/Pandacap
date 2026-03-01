using Pandacap.ConfigurationObjects;
using Pandacap.Data;

namespace Pandacap.HighLevel.PlatformLinks
{
    public record MastodonLink(
        ApplicationInformation ApplicationInformation,
        string Host) : IPlatformLink
    {
        public PlatformLinkCategory Category => PlatformLinkCategory.ActivityPub;

        public string Username => ApplicationInformation.ActivityPubWebFingerHandle;

        public string? ViewProfileUrl => null;

        public string? GetViewPostUrl(Post post) => null;
    }
}
