using Microsoft.EntityFrameworkCore;
using Pandacap.Data;
using Pandacap.LowLevel;

namespace Pandacap
{
    public class DeliveryInboxCollector(PandacapDbContext context)
    {
        public async Task<HashSet<string>> GetDeliveryInboxesAsync(
            Post? post = null,
            bool includeFollows = false,
            CancellationToken cancellationToken = default)
        {
            var followers = await context.Followers
                .Select(f => new { f.Inbox, f.SharedInbox })
                .ToListAsync(cancellationToken);

            var follows = includeFollows
                ? await context.Follows
                    .Select(f => new { f.Inbox, f.SharedInbox })
                    .ToListAsync(cancellationToken)
                : [];

            var excludeHosts = post?.BridgyFed == false
                ? BridgyFed.Domains
                : [];

            var set = new[] { followers, follows }
                .SelectMany(f => f)
                .Select(f => f.SharedInbox ?? f.Inbox)
                .Where(inbox =>
                    Uri.TryCreate(inbox, UriKind.Absolute, out Uri? uri)
                    && !excludeHosts.Contains(uri.Host))
                .ToHashSet();

            return set;
        }
    }
}
