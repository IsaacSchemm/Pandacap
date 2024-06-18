namespace Pandacap.Data
{
    public interface IPost
    {
        string Id { get; }

        string? Username { get; }
        string? Usericon { get; }

        string DisplayTitle { get; }
        DateTimeOffset Timestamp { get; }
        string? LinkUrl { get; }

        IEnumerable<IPostImage> Images { get; }
    }
}
