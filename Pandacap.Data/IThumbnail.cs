namespace Pandacap.Data
{
    public interface IThumbnail
    {
        IEnumerable<IThumbnailRendition> Renditions { get; }
        string? AltText { get; }
    }
}
