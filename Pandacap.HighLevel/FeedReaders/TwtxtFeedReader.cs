using Pandacap.Clients;
using Pandacap.Data;
using System.Net;

namespace Pandacap.HighLevel.FeedReaders
{
    internal class TwtxtFeedReader(
        IHttpClientFactory httpClientFactory
    ) : IFeedReader
    {
        public async IAsyncEnumerable<GeneralInboxItem> ReadFeedAsync(
            string uri,
            string? contentType)
        {
            if (contentType != null && contentType != "text/plain")
                yield break;

            using var client = httpClientFactory.CreateClient();
            using var resp = await client.GetAsync(uri);

            var content = await resp.EnsureSuccessStatusCode().Content.ReadAsByteArrayAsync();
            var feed = TwtxtReader.ReadFeed(content);

            foreach (var item in feed.twts)
            {
                yield return new()
                {
                    FeedIconUrl = feed.metadata.avatar.HeadOrDefault,
                    FeedTitle = feed.metadata.nick.HeadOrDefault,
                    FeedWebsiteUrl = uri,
                    HtmlDescription = WebUtility.HtmlEncode(item.text),
                    Timestamp = item.timestamp,
                    Url = uri,
                    Id = Guid.NewGuid()
                };
            }
        }
    }
}
