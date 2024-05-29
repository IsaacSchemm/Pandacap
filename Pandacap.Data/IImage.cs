namespace Pandacap.Data
{
    public interface IImage
    {
        string? ThumbnailUrl { get; }
        string? ThumbnailSrcset { get; }
        string? AltText { get; }
    }
}
