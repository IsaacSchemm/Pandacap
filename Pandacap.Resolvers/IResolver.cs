using Pandacap.Resolvers.Models;

namespace Pandacap.Resolvers
{
    public interface IResolver
    {
        Task<ResolverResult> ResolveAsync(
            string url,
            CancellationToken cancellationToken);
    }
}
