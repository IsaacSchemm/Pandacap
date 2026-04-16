using Pandacap.PlatformLinks.Interfaces;
using Pandacap.PlatformLinks.Models;

namespace Pandacap.PlatformLinks.LinkTemplates
{
    public record DeviantArtLinkTemplate(
        string Username) : ILinkTemplate
    {
        public PlatformLinkCategory Category => PlatformLinkCategory.DeviantArt;

        public string? IconFilename => "deviantart.png";

        public string? PlatformName => "DeviantArt";

        public string? GetUrl(PlatformLinkContext context) =>
            context is PlatformLinkContext.Post post ? post.Item.DeviantArtUrl
            : context.IsProfile ? $"https://www.deviantart.com/{Uri.EscapeDataString(Username)}"
            : null;
    }
}
