namespace Pandacap.UI.Elements
{
    public interface IInboxPost : IPost
    {
        DateTimeOffset? DismissedAt { get; set; }
        bool IsPodcast { get; }
        bool IsShare { get; }
    }
}
