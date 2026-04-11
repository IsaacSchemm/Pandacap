using Pandacap.PlatformLinks.Interfaces;

namespace Pandacap.PlatformLinks
{
    public record DeviantArtPlatformLink(
        string Username) : IPlatformLink
    {
        public PlatformLinkCategory Category => PlatformLinkCategory.DeviantArt;

        public string? PlatformName => "DeviantArt";

        public string? ViewProfileUrl => $"https://www.deviantart.com/{Uri.EscapeDataString(Username)}";

        public string? IconFilename => "deviantart.png";

        public string? GetViewPostUrl(IPlatformLinkPostSource post) => post.DeviantArtUrl;
    }
}
