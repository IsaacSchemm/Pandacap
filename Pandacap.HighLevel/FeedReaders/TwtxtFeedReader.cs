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

            var respContentType = resp.EnsureSuccessStatusCode().Content.Headers.ContentType?.MediaType;
            if (respContentType is string respMediaType && respMediaType != "text/plain")
                yield break;

            var content = await resp.EnsureSuccessStatusCode().Content.ReadAsByteArrayAsync();
            var feed = Twtxt.Reader.ReadFeed(content);

            foreach (var item in feed.twts)
            {
                yield return new()
                {
                    FeedIconUrl = feed.metadata.avatar.HeadOrDefault,
                    FeedTitle = feed.metadata.nick.HeadOrDefault,
                    FeedWebsiteUrl = uri,
                    TextBody = Twtxt.Reader.ToPlainText(item.text),
                    HtmlBody = Twtxt.Reader.ToHTML(item.text),
                    Timestamp = item.timestamp,
                    Url = uri,
                    Id = Guid.NewGuid()
                };
            }
        }
    }
}
