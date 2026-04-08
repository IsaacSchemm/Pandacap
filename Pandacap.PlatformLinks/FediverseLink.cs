using Pandacap.ActivityPub.Static;
using Pandacap.PlatformLinks.Interfaces;

namespace Pandacap.PlatformLinks
{
    public record FediverseLink(
        string Host,
        string PlatformName) : IPlatformLink, IActivityPubProfileLink
    {
        public PlatformLinkCategory Category => PlatformLinkCategory.ActivityPub;

        public string Username => $"@{ActivityPubHostInformation.Username}@{ActivityPubHostInformation.ApplicationHostname}";

        public string? ViewProfileUrl => null;

        public string? GetViewPostUrl(IPlatformLinkPostSource post) => null;
    }
}
