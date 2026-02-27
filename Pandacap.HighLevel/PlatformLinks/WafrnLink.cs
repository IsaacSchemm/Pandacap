using Pandacap.ConfigurationObjects;
using Pandacap.Data;

namespace Pandacap.HighLevel.PlatformLinks
{
    public record WafrnLink(
        ApplicationInformation ApplicationInformation,
        string Host) : IPlatformLink
    {
        public PlatformLinkCategory Category => PlatformLinkCategory.ActivityPub;

        public string PlatformName => Host;

        public string Username => $"@{ApplicationInformation.Username}@{ApplicationInformation.ApplicationHostname}";

        public string? IconUrl => $"https://{Host}/favicon.ico";

        public string? ViewProfileUrl => $"https://{Host}/blog/{Username}";

        public string? GetViewPostUrl(Post post) => null;
    }
}
