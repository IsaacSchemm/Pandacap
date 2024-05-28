namespace Pandacap.Data
{
    public interface IInboxPost
    {
        string Id { get; }

        string? Username { get; }
        string? Usericon { get; }

        string? DisplayTitle { get; }
        DateTimeOffset Timestamp { get; }
        string? LinkUrl { get; }

        DateTimeOffset? DismissedAt { get; }
    }
}
