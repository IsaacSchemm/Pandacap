using Pandacap.Data;

namespace Pandacap.HighLevel.PlatformLinks
{
    public record FurAffinityPlatformLink(
        string Username) : IPlatformLink
    {
        public PlatformLinkCategory Category => PlatformLinkCategory.External;

        public string PlatformName => "Fur Affinity";

        public string? IconUrl => "https://sfw.furaffinity.net/themes/beta/img/favicon.ico";

        public string? ViewProfileUrl => $"https://www.furaffinity.net/user/{Uri.EscapeDataString(Username)}";

        public string? GetViewPostUrl(Post post) =>
            post.FurAffinitySubmissionId is int submissionId ? $"https://www.furaffinity.net/view/{submissionId}/"
            : post.FurAffinityJournalId is int journalId ? $"https://www.furaffinity.net/journal/{journalId}/"
            : null;
    }
}
