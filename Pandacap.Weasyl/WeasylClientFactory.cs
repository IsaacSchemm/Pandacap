using Pandacap.Weasyl.Interfaces;
using Pandacap.Weasyl.Scraping.Interfaces;

namespace Pandacap.Weasyl
{
    internal class WeasylClientFactory(
        WeasylHttpHandlerProvider weasylHttpHandlerProvider,
        IWeasylScraper weasylScraper,
        WeasylConfiguration weasylConfiguration) : IWeasylClientFactory
    {
        public IWeasylClient CreateWeasylClient() =>
            new WeasylClient(
                weasylHttpHandlerProvider.GetOrCreateHandler(),
                weasylConfiguration.WeasylApiKey,
                weasylScraper);
    }
}
