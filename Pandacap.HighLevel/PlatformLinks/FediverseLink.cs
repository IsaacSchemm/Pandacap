using Pandacap.ConfigurationObjects;
using Pandacap.Database;

namespace Pandacap.HighLevel.PlatformLinks
{
    public record FediverseLink(
        ApplicationInformation ApplicationInformation,
        string Host,
        string PlatformName) : IPlatformLink
    {
        public PlatformLinkCategory Category => PlatformLinkCategory.ActivityPub;

        public string Username => ApplicationInformation.ActivityPubWebFingerHandle;

        public string? ViewProfileUrl => null;

        public string? GetViewPostUrl(Post post) => null;
    }
}
