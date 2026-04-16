using Pandacap.ActivityPub.Static;
using Pandacap.PlatformLinks.Interfaces;

namespace Pandacap.PlatformLinks.LinkTemplates
{
    public record FediverseLinkTemplate(
        string PlatformName,
        string? IconFilename = null) : ILinkTemplate
    {
        public PlatformLinkCategory Category => PlatformLinkCategory.ActivityPub;

        public string Username => $"@{ActivityPubHostInformation.Username}@{ActivityPubHostInformation.ApplicationHostname}";

        public string? GetViewPostUrl(IPlatformLinkPostSource post) => null;

        public string? GetViewProfileUrl() => null;
    }
}
