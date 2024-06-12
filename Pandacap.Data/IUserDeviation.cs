namespace Pandacap.Data
{
    public interface IUserDeviation
    {
        Guid Id { get; }
        string? LinkUrl { get; }
        string? Title { get; }
        bool FederateTitle { get; set; }
        DateTimeOffset PublishedTime { get; }
        bool IsMature { get; }
        string? Description { get; }
        IEnumerable<string> Tags { get; }
    }
}
