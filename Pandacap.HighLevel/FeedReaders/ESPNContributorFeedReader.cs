using Pandacap.Clients;
using Pandacap.Data;

namespace Pandacap.HighLevel.FeedReaders
{
    public class ESPNContributorFeedReader(
        IHttpClientFactory httpClientFactory
    ) : IFeedReader
    {
        public async IAsyncEnumerable<GeneralInboxItem> ReadFeedAsync(
            string uri,
            string? contentType)
        {
            if (!uri.StartsWith("https://www.espn.com/contributor/"))
                yield break;

            if (contentType != "text/html")
                yield break;

            using var client = httpClientFactory.CreateClient();
            using var resp = await client.GetAsync(uri);

            var html = await resp.EnsureSuccessStatusCode().Content.ReadAsStringAsync();

            int index1 = html.IndexOf("window['__espnfitt__']=");
            while (html[index1] != '{')
                index1++;

            int index2 = html.IndexOf(";</script>");
            while (html[index2] != '}')
                index2--;

            var json = html.Substring(index1, index2 - index1 + 1);
            var data = ESPN.ParseContributorData(json);

            foreach (var item in data.page.content.contributor.feed)
                yield return new GeneralInboxItem
                {
                    FeedIconUrl = item.hdr?.image?.url,
                    FeedWebsiteUrl = uri,
                    FeedTitle = item.byline,
                    Id = Guid.NewGuid(),
                    TextBody = item.description,
                    Timestamp = item.published,
                    Url = item.absoluteLink
                };
        }
    }
}
