using Pandacap.Weasyl.Interfaces;
using Pandacap.Weasyl.Scraping.Interfaces;

namespace Pandacap.Weasyl
{
    internal class WeasylClientFactory(
        IWeasylHttpHandlerProvider weasylHttpHandlerProvider,
        IWeasylScraper weasylScraper) : IWeasylClientFactory
    {
        public IWeasylClient CreateWeasylClient(string apiKey, string phpProxyHost) =>
            new WeasylClient(
                weasylHttpHandlerProvider.GetOrCreateHandler(),
                apiKey,
                phpProxyHost,
                weasylScraper);
    }
}
