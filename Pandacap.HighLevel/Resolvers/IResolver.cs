using Pandacap.Resolvers;

namespace Pandacap.HighLevel.Resolvers
{
    public interface IResolver
    {
        Task<ResolverResult> ResolveAsync(
            string url,
            CancellationToken cancellationToken);
    }
}
