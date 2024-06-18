namespace Pandacap.Data
{
    public interface IPostImage
    {
        string? Url { get; }
        string? AltText { get; }
        IEnumerable<IThumbnailRendition> Thumbnails { get; }
    }
}
