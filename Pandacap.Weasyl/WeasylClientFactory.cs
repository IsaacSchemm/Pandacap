using Pandacap.Weasyl.Interfaces;
using Pandacap.Weasyl.Scraping.Interfaces;

namespace Pandacap.Weasyl
{
    internal class WeasylClientFactory(
        IWeasylHttpHandlerProvider weasylHttpHandlerProvider,
        IWeasylScraper weasylScraper,
        WeasylConfiguration weasylConfiguration) : IWeasylClientFactory
    {
        public IWeasylClient CreateWeasylClient(string apiKey) =>
            new WeasylClient(
                weasylHttpHandlerProvider.GetOrCreateHandler(),
                apiKey,
                weasylConfiguration.WeasylProxyHost.Host,
                weasylScraper);
    }
}
