namespace Pandacap.Data
{
    public interface IDeviation
    {
        Guid Id { get; }
        string? Url { get; }
        string? Username { get; }
        string? Usericon { get; }
        string? Title { get; }
        DateTimeOffset PublishedTime { get; }
        bool IsMature { get; }
        string? Description { get; }
        IEnumerable<string> Tags { get; }
        IDeviationImage? Image { get; }
    }
}
