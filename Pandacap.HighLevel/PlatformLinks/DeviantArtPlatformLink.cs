using Pandacap.Data;

namespace Pandacap.HighLevel.PlatformLinks
{
    public record DeviantArtPlatformLink(
        string Username) : IPlatformLink
    {
        public PlatformLinkCategory Category => PlatformLinkCategory.External;

        public string Host => "www.deviantart.com";

        public string? IconUrl => null;

        public string? ViewProfileUrl => $"https://www.deviantart.com/{Uri.EscapeDataString(Username)}";

        public string? GetViewPostUrl(Post post) => post.DeviantArtUrl;
    }
}
