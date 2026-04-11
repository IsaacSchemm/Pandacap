using Pandacap.PlatformLinks.Interfaces;

namespace Pandacap.PlatformLinks
{
    public record FurAffinityPlatformLink(
        string Username) : IPlatformLink
    {
        public PlatformLinkCategory Category => PlatformLinkCategory.FurAffinity;

        public string? PlatformName => "Fur Affinity";

        public string? ViewProfileUrl => $"https://www.furaffinity.net/user/{Uri.EscapeDataString(Username)}";

        public string? IconFilename => "furaffinity.ico";

        public string? GetViewPostUrl(IPlatformLinkPostSource post) =>
            post.FurAffinitySubmissionId is int submissionId ? $"https://www.furaffinity.net/view/{submissionId}/"
            : post.FurAffinityJournalId is int journalId ? $"https://www.furaffinity.net/journal/{journalId}/"
            : null;
    }
}
