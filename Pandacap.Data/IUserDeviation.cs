namespace Pandacap.Data
{
    public interface IUserDeviation
    {
        Guid Id { get; }
        string? LinkUrl { get; }
        string? Title { get; }
        DateTimeOffset PublishedTime { get; }
        bool IsMature { get; }
        string? Description { get; }
        IEnumerable<string> Tags { get; }
    }
}
