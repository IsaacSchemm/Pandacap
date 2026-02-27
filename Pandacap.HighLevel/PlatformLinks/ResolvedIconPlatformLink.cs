using Pandacap.Data;

namespace Pandacap.HighLevel.PlatformLinks
{
    public class ResolvedIconPlatformLink(
        IPlatformLink UnderlyingLink,
        string iconUrl) : IPlatformLink
    {
        public PlatformLinkCategory Category => UnderlyingLink.Category;

        public string Host => UnderlyingLink.Host;

        public string Username => UnderlyingLink.Username;

        public string? IconUrl => iconUrl;

        public string? ViewProfileUrl => UnderlyingLink.ViewProfileUrl;

        public string? GetViewPostUrl(Post post) => UnderlyingLink.GetViewPostUrl(post);
    }
}
