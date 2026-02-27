using Pandacap.ConfigurationObjects;
using Pandacap.Data;

namespace Pandacap.HighLevel.PlatformLinks
{
    public record MastodonLink(
        ApplicationInformation ApplicationInformation,
        string Host) : IPlatformLink
    {
        public PlatformLinkCategory Category => PlatformLinkCategory.ActivityPub;

        public string PlatformName => Host;

        public string Username => $"@{ApplicationInformation.Username}@{ApplicationInformation.ApplicationHostname}";

        public string? IconUrl => $"/images/mastodon.svg";

        public string? ViewProfileUrl => $"https://{Host}/{Username}";

        public string? GetViewPostUrl(Post post) => null;
    }
}
