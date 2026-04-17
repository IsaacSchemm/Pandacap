using Pandacap.PlatformLinks.Factories;
using Pandacap.PlatformLinks.Interfaces;
using System.Runtime.CompilerServices;

namespace Pandacap.PlatformLinks
{
    internal class PlatformLinkProvider(
        IPlatformLinkProfileProvider platformLinkProfileProvider) : IPlatformLinkProvider
    {
        private async IAsyncEnumerable<IPlatformLink> GetPostLinksAsync(
            IPlatformLinkPost post,
            [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            var profile = await platformLinkProfileProvider.GetProfileInformationAsync(cancellationToken);
            foreach (var link in PlatformLinkFactory.GetAllPostLinks(profile, post))
                yield return link;
        }

        private async IAsyncEnumerable<IPlatformLink> GetProfileLinksAsync(
            [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            var profile = await platformLinkProfileProvider.GetProfileInformationAsync(cancellationToken);
            foreach (var link in PlatformLinkFactory.GetAllPlatforms(profile))
                yield return link;
        }

        IAsyncEnumerable<IPlatformLink> IPlatformLinkProvider.GetPostLinksAsync(IPlatformLinkPost post) =>
            GetPostLinksAsync(post);

        IAsyncEnumerable<IPlatformLink> IPlatformLinkProvider.GetProfileLinksAsync() =>
            GetProfileLinksAsync();
    }
}
