namespace Pandacap.Data
{
    public interface IInboxImage
    {
        string? ThumbnailUrl { get; }
        string? ThumbnailSrcset { get; }
        string? AltText { get; }
    }
}
