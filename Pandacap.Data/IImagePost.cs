namespace Pandacap.Data
{
    public interface IImagePost : IPost
    {
        IEnumerable<IImage> Images { get; }
    }
}
