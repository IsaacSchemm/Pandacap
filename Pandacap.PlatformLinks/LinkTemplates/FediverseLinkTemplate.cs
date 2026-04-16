using Pandacap.ActivityPub.Static;
using Pandacap.PlatformLinks.Interfaces;
using Pandacap.PlatformLinks.Models;

namespace Pandacap.PlatformLinks.LinkTemplates
{
    public record FediverseLinkTemplate(
        string PlatformName,
        string? IconFilename = null) : ILinkTemplate
    {
        public PlatformLinkCategory Category => PlatformLinkCategory.ActivityPub;

        public string Username => $"@{ActivityPubHostInformation.Username}@{ActivityPubHostInformation.ApplicationHostname}";

        public string? GetUrl(PlatformLinkContext _) => null;
    }
}
