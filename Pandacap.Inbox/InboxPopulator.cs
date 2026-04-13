using Pandacap.Inbox.Interfaces;

namespace Pandacap.Inbox
{
    public class InboxPopulator(
        IEnumerable<IInboxSourceFactory> inboxSourceFactories) : IInboxPopulator
    {
        public async Task PopulateInboxAsync(CancellationToken cancellationToken)
        {
            List<Exception> exceptions = [];

            var sources = await inboxSourceFactories
                .ToAsyncEnumerable()
                .SelectMany(factory => factory.GetInboxSourcesForPlatformAsync())
                .ToListAsync(cancellationToken);

            foreach (var source in sources)
            {
                try
                {
                    await source.ImportNewPostsAsync(cancellationToken);
                }
                catch (Exception e)
                {
                    exceptions.Add(e);
                }
            }

            if (exceptions.Count > 0)
                throw new AggregateException(exceptions);
        }
    }
}
