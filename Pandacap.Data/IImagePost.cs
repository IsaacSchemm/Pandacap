namespace Pandacap.Data
{
    public interface IImagePost : IPost
    {
        IEnumerable<IThumbnail> Thumbnails { get; }
    }
}
