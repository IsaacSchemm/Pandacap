namespace Pandacap.PlatformLinks.Interfaces
{
    public interface IPlatformLink
    {
        string Category { get; }
        string IconFilename { get; }
        string PlatformName { get; }
        string? Text { get; }
        string? Url { get; }
    }
}
