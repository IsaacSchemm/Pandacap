namespace Pandacap.Data
{
    public interface IThumbnail
    {
        string? ThumbnailUrl { get; }
        string? ThumbnailSrcset { get; }
        string? AltText { get; }
    }
}
