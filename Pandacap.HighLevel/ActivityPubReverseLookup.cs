using Microsoft.EntityFrameworkCore;
using Microsoft.FSharp.Collections;
using Pandacap.Data;
using Pandacap.LowLevel;
using System.Text.RegularExpressions;

namespace Pandacap.HighLevel
{
    public partial class ActivityPubReverseLookup(
        PandacapDbContext context,
        IdMapper mapper)
    {
        public async IAsyncEnumerable<DiscoveredPost> FindPostsAsync(
            FSharpSet<string> activityPubIds)
        {
            FSharpSet<Guid> discoveredGuids = [
                .. activityPubIds
                    .SelectMany(url => GuidPattern.Matches(url).Select(m => m.Value))
                    .Select(Guid.Parse)
            ];

            await foreach (var post in context.Posts
                .Where(p => discoveredGuids.Contains(p.Id))
                .AsAsyncEnumerable())
            {
                string objectId = mapper.GetObjectId(post);
                if (activityPubIds.Contains(mapper.GetObjectId(post)))
                    yield return new(post.Id, objectId);
            }

            await foreach (var post in context.AddressedPosts
                .Where(p => discoveredGuids.Contains(p.Id))
                .AsAsyncEnumerable())
            {
                string objectId = mapper.GetObjectId(post);
                if (activityPubIds.Contains(mapper.GetObjectId(post)))
                    yield return new(post.Id, objectId);
            }
        }

        public record DiscoveredPost(
            Guid Id,
            string ActivityPubId);

        private static readonly Regex GuidPattern = GetGuidPattern();

        [GeneratedRegex("[0-9A-F]{8}-[0-9A-F]{4}-[0-9A-F]{4}-[0-9A-F]{4}-[0-9A-F]{12}", RegexOptions.IgnoreCase)]
        private static partial Regex GetGuidPattern();
    }
}
