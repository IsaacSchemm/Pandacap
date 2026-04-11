using Pandacap.PlatformLinks.Interfaces;

namespace Pandacap.PlatformLinks.LinkTemplates
{
    public record WeasylPlatformLinkTemplate(
        string Username) : ILinkTemplate
    {
        public PlatformLinkCategory Category => PlatformLinkCategory.Weasyl;

        public string? IconFilename => "weasyl.svg";

        public string? PlatformName => "Weasyl";

        public string? GetViewPostUrl(IPlatformLinkPostSource post) =>
            post.WeasylSubmitId is int submitId ? $"https://www.weasyl.com/~{Uri.EscapeDataString(Username)}/submissions/{submitId}"
            : post.WeasylJournalId is int journalId ? $"https://www.weasyl.com/journal/{journalId}/"
            : null;

        public string? GetViewProfileUrl() =>
            $"https://www.weasyl.com/~{Uri.EscapeDataString(Username)}";
    }
}
