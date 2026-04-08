namespace Pandacap.PlatformLinks.Interfaces
{
    public interface IPlatformLink
    {
        PlatformLinkCategory Category { get; }
        string Host { get; }
        string Username { get; }

        string? PlatformName { get; }
        string? ViewProfileUrl { get; }

        string? GetViewPostUrl(IPlatformLinkPostSource post);
    }
}
