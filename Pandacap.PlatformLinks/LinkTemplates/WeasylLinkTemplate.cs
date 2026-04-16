using Pandacap.PlatformLinks.Interfaces;
using Pandacap.PlatformLinks.Models;

namespace Pandacap.PlatformLinks.LinkTemplates
{
    public record WeasylLinkTemplate(
        string Username) : ILinkTemplate
    {
        public PlatformLinkCategory Category => PlatformLinkCategory.Weasyl;

        public string? IconFilename => "weasyl.svg";

        public string? PlatformName => "Weasyl";

        public string? GetUrl(PlatformLinkContext context)
        {
            if (context is PlatformLinkContext.Post post)
            {
                if (post.Item.WeasylSubmitId is int submitId)
                    return $"https://www.weasyl.com/~{Uri.EscapeDataString(Username)}/submissions/{submitId}";
                if (post.Item.WeasylJournalId is int journalId)
                    return $"https://www.weasyl.com/journal/{journalId}/";
            }

            if (context.IsProfile)
                return $"https://www.weasyl.com/~{Uri.EscapeDataString(Username)}";

            return null;
        }
    }
}
