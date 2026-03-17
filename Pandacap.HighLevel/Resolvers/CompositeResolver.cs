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
                    var res = await resolver.ResolveAsync(url, cancellationToken);
                    if (!res.IsNone)
                        return res;
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
