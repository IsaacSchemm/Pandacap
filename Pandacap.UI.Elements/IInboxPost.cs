namespace Pandacap.UI.Elements
{
    public interface IInboxPost : IPost
    {
        DateTimeOffset? DismissedAt { get; }
        bool IsPodcast { get; }
        bool IsShare { get; }
    }
}
