using Pandacap.Database;

namespace Pandacap.HighLevel.FeedReaders
{
    public interface IFeedReader
    {
        IAsyncEnumerable<GeneralInboxItem> ReadFeedAsync(
            string uri,
            string? contentType);
    }
}
