using Pandacap.Data;

namespace Pandacap.HighLevel.PlatformLinks
{
    public interface IPlatformLink
    {
        PlatformLinkCategory Category { get; }
        string Host { get; }
        string Username { get; }

        string? IconUrl { get; }
        string? ViewProfileUrl { get; }

        string? GetViewPostUrl(Post post);
    }
}
