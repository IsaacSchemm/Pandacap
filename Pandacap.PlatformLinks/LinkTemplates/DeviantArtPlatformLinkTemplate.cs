using Pandacap.PlatformLinks.Interfaces;

namespace Pandacap.PlatformLinks.LinkTemplates
{
    public record DeviantArtPlatformLinkTemplate(
        string Username) : ILinkTemplate
    {
        public PlatformLinkCategory Category => PlatformLinkCategory.DeviantArt;

        public string? IconFilename => "deviantart.png";

        public string? PlatformName => "DeviantArt";

        public string? GetViewPostUrl(IPlatformLinkPostSource post) => post.DeviantArtUrl;

        public string? GetViewProfileUrl() =>
            $"https://www.deviantart.com/{Uri.EscapeDataString(Username)}";
    }
}
