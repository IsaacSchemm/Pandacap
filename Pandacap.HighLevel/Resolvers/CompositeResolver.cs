using Pandacap.Resolvers;

namespace Pandacap.HighLevel.Resolvers
{
    public class CompositeResolver(
        IEnumerable<IResolver> resolvers)
    {
        public async Task<ResolverResult?> ResolveAsync(
            string url,
            CancellationToken cancellationToken)
        {
            var errors = new List<Exception>();

            foreach (var resolver in resolvers)
            {
                try
                {
                    await foreach (var result in resolver.ResolveAsync(
                        url,
                        cancellationToken))
                    {
                        return result;
                    }
                }
                catch (Exception ex)
                {
                    errors.Add(ex);
                }
            }

            if (errors.Count > 0)
                throw new AggregateException($"Could not resolve URL: {url}", errors);

            return null;
        }
    }
}
