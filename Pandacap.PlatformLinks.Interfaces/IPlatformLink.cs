namespace Pandacap.PlatformLinks.Interfaces
{
    public interface IPlatformLink
    {
        PlatformLinkCategory Category { get; }
        string? IconFilename { get; }
        string? PlatformName { get; }
        string? Username { get; }
        string? Url { get; }
    }
}
