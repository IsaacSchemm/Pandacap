using Pandacap.Data;

namespace Pandacap.HighLevel.PlatformLinks
{
    public record DeviantArtPlatformLink(
        string Username) : IPlatformLink
    {
        public PlatformLinkCategory Category => PlatformLinkCategory.External;

        public string PlatformName => "DeviantArt";

        public string? IconUrl => "https://st.deviantart.net/eclipse/icons/da_favicon_v2.ico";

        public string? ViewProfileUrl => $"https://www.deviantart.com/{Uri.EscapeDataString(Username)}";

        public string? GetViewPostUrl(Post post) => post.DeviantArtUrl;
    }
}
