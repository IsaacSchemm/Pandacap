using Pandacap.Inbox.Interfaces;

namespace Pandacap.Inbox
{
    public class InboxPopulator(
        IEnumerable<IInboxSource> inboxSources) : IInboxPopulator
    {
        public async Task PopulateInboxAsync(CancellationToken cancellationToken)
        {
            List<Exception> exceptions = [];

            foreach (var source in inboxSources)
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
