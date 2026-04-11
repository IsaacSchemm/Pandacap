using Pandacap.PlatformLinks.Interfaces;

namespace Pandacap.PlatformLinks.LinkTemplates
{
    public interface ILinkTemplate
    {
        PlatformLinkCategory Category { get; }

        string? IconFilename { get; }
        string? PlatformName { get; }
        string? Username { get; }

        string? GetViewProfileUrl();
        string? GetViewPostUrl(IPlatformLinkPostSource post);
    }
}
