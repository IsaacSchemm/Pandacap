using Pandacap.Weasyl.Interfaces;
using Pandacap.Weasyl.Scraping.Interfaces;

namespace Pandacap.Weasyl
{
    internal class WeasylClientFactory(
        WeasylHttpHandlerProvider weasylHttpHandlerProvider,
        IWeasylScraper weasylScraper) : IWeasylClientFactory
    {
        public IWeasylClient CreateWeasylClient(IWeasylCredentials weasylCredentials) =>
            new WeasylClient(
                weasylHttpHandlerProvider.GetOrCreateHandler(),
                weasylCredentials.WeasylApiKey,
                weasylScraper);
    }
}
