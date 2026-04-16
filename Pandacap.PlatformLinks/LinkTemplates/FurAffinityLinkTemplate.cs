using Pandacap.PlatformLinks.Interfaces;
using Pandacap.PlatformLinks.Models;

namespace Pandacap.PlatformLinks.LinkTemplates
{
    public record FurAffinityLinkTemplate(
        string Username) : ILinkTemplate
    {
        public PlatformLinkCategory Category => PlatformLinkCategory.FurAffinity;

        public string? IconFilename => "furaffinity.ico";

        public string? PlatformName => "Fur Affinity";

        public string? GetUrl(PlatformLinkContext context) {
            if (context is PlatformLinkContext.Post post)
            {
                if (post.Item.FurAffinitySubmissionId is int submissionId)
                    return $"https://www.furaffinity.net/view/{submissionId}/";
                if (post.Item.FurAffinityJournalId is int journalId)
                    return $"https://www.furaffinity.net/view/{journalId}/";
            }

            if (context.IsProfile)
                return $"https://www.deviantart.com/{Uri.EscapeDataString(Username)}";

            return null;
        }
    }
}
