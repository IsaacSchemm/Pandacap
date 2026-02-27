using Pandacap.Data;

namespace Pandacap.HighLevel.PlatformLinks
{
    public record FurAffinityPlatformLink(
        string Username) : IPlatformLink
    {
        public PlatformLinkCategory Category => PlatformLinkCategory.External;

        public string Host => "www.furaffinity.net";

        public string? IconUrl => null;

        public string? ViewProfileUrl => $"https://www.furaffinity.net/user/{Uri.EscapeDataString(Username)}";

        public string? GetViewPostUrl(Post post) =>
            post.FurAffinitySubmissionId is int submissionId ? $"https://www.furaffinity.net/view/{submissionId}/"
            : post.FurAffinityJournalId is int journalId ? $"https://www.furaffinity.net/journal/{journalId}/"
            : null;
    }
}
