using Microsoft.Extensions.DependencyInjection;
using Pandacap.VectorSearch.Interfaces;

namespace Pandacap.VectorSearch
{
    public static class DependencyInjectionExtensions
    {
        public static IServiceCollection AddVectorSearch(this IServiceCollection serviceCollection) =>
            serviceCollection
            .AddScoped<EmbeddingsProvider>()
            .AddScoped<IVectorSearchIndexClient, VectorSearchIndexClient>();
    }
}
