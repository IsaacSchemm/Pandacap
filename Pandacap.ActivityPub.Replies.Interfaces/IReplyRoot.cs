namespace Pandacap.ActivityPub.Replies.Interfaces
{
    public interface IReplyRoot
    {
        DateTimeOffset CreatedAt { get; }
        string CreatedBy { get; }
        string? HtmlContent { get; }
        string? Name { get; }
        string ObjectId { get; }
        bool Remote { get; }
        bool Sensitive { get; }
        string? Summary { get; }
        string? Usericon { get; }
        string? Username { get; }
    }
}
