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

            var follows = await context.Follows
                .Select(f => new { f.Inbox, f.SharedInbox })
                .ToListAsync(cancellationToken);

            IEnumerable<string> excludeHosts = post?.BridgyFed == false
                ? ["bsky.brid.gy", "web.brid.gy"]
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
