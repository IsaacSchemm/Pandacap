using Pandacap.Resolvers;

namespace Pandacap.HighLevel.Resolvers
{
    public interface IResolver
    {
        IAsyncEnumerable<ResolverResult> ResolveAsync(
            string url,
            CancellationToken cancellationToken);
    }
}
