using Microsoft.Extensions.DependencyInjection;
using Pandacap.PostCreation.Interfaces;

namespace Pandacap.PostCreation
{
    public static class DependencyInjectionExtensions
    {
        public static IServiceCollection AddPostCreation(this IServiceCollection serviceCollection) =>
            serviceCollection.AddScoped<IPostCreator, PostCreator>();
    }
}
