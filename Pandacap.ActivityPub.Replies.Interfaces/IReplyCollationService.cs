namespace Pandacap.ActivityPub.Replies.Interfaces
{
    public interface IReplyCollationService
    {
        Task<bool> IsOriginalPostStoredAsync(
            string id,
            CancellationToken cancellationToken);

        Task<IReply> AddRepliesAsync(
            IReplyRoot root,
            CancellationToken cancellationToken);

        IAsyncEnumerable<IReply> CollectRepliesAsync(
            string originalObjectId);
    }
}
