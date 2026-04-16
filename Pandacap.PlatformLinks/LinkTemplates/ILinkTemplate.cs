using Pandacap.PlatformLinks.Interfaces;
using Pandacap.PlatformLinks.Models;

namespace Pandacap.PlatformLinks.LinkTemplates
{
    public interface ILinkTemplate
    {
        PlatformLinkCategory Category { get; }

        string? IconFilename { get; }
        string? PlatformName { get; }
        string? Username { get; }

        string? GetUrl(PlatformLinkContext platformLinkContext);
    }
}
