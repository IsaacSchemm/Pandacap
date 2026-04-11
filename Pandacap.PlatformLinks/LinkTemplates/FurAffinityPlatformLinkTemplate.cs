using Pandacap.PlatformLinks.Interfaces;

namespace Pandacap.PlatformLinks.LinkTemplates
{
    public record FurAffinityPlatformLinkTemplate(
        string Username) : ILinkTemplate
    {
        public PlatformLinkCategory Category => PlatformLinkCategory.FurAffinity;

        public string? IconFilename => "furaffinity.ico";

        public string? PlatformName => "Fur Affinity";

        public string? GetViewPostUrl(IPlatformLinkPostSource post) =>
            post.FurAffinitySubmissionId is int submissionId ? $"https://www.furaffinity.net/view/{submissionId}/"
            : post.FurAffinityJournalId is int journalId ? $"https://www.furaffinity.net/journal/{journalId}/"
            : null;

        public string? GetViewProfileUrl() =>
            $"https://www.furaffinity.net/user/{Uri.EscapeDataString(Username)}";
    }
}
