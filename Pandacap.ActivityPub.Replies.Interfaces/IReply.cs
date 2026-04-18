namespace Pandacap.ActivityPub.Replies.Interfaces
{
    public interface IReply
    {
        IReplyRoot Root { get; }
        IReadOnlyList<IReply> Replies { get; }
    }
}
