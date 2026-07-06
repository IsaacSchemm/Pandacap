using Pandacap.Constants;
using Pandacap.Rss.Interfaces;

namespace Pandacap.Rss
{
    internal class FeedRequestHandler(
        IHttpClientFactory httpClientFactory) : IFeedRequestHandler
    {
        public async Task<HttpResponseMessage> GetAsync(string uri, CancellationToken cancellationToken)
        {
            using var client = httpClientFactory.CreateClient();
            client.DefaultRequestHeaders.UserAgent.ParseAdd(UserAgentInformation.UserAgent);
            return await client.GetAsync(uri, cancellationToken);
        }
    }
}
