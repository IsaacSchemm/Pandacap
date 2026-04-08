using Pandacap.PlatformLinks.Interfaces;

namespace Pandacap.PlatformLinks
{
    public record WeasylPlatformLink(
        string Username) : IPlatformLink
    {
        public PlatformLinkCategory Category => PlatformLinkCategory.External;

        public string Host => "www.weasyl.com";

        public string? PlatformName => "Weasyl";

        public string? ViewProfileUrl => $"https://www.weasyl.com/~{Uri.EscapeDataString(Username)}";

        public string? GetViewPostUrl(IPlatformLinkPostSource post) =>
            post.WeasylSubmitId is int submitId ? $"https://www.weasyl.com/~{Uri.EscapeDataString(Username)}/submissions/{submitId}"
            : post.WeasylJournalId is int journalId ? $"https://www.weasyl.com/journal/{journalId}/"
            : null;
    }
}
