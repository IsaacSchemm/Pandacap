using Pandacap.ConfigurationObjects;
using Pandacap.LowLevel.Txt;

namespace Pandacap.HighLevel
{
    public class TwtxtClient(
        ApplicationInformation appInfo,
        IHttpClientFactory httpClientFactory)
    {
        public string MyFeedUrl => $"{appInfo.ApplicationHostname}/Twtxt";

        public async Task<Feed> ReadFeedAsync(Uri uri)
        {
            var appName = UserAgentInformation.ApplicationName;
            var ver = UserAgentInformation.VersionNumber;

            using var client = httpClientFactory.CreateClient();
            client.DefaultRequestHeaders.UserAgent.ParseAdd($"{appName}/{ver} (+{MyFeedUrl}; @{appInfo.Username})");

            using var resp = await client.GetAsync(uri);

            var data = await resp.EnsureSuccessStatusCode().Content.ReadAsByteArrayAsync();

            var feed = FeedReader.ReadFeed(data);

            if (feed.metadata.url.IsEmpty)
            {
                feed = feed.WithUrl([uri]);
            }

            return feed;
        }
    }
}
