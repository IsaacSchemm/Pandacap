using Microsoft.Extensions.Caching.Memory;
using Pandacap.Html;

namespace Pandacap.HighLevel.PlatformLinks
{
    public class PlatformIconProvider(
        IHttpClientFactory httpClientFactory,
        IMemoryCache memoryCache)
    {
        private const string KEY = "b593956e-8d55-48c7-81e2-8ffd43fc412c";

        public async Task<string?> ResolveIconAsync(
            string host,
            CancellationToken cancellationToken = default)
        =>
            await memoryCache.GetOrCreateAsync<string?>(
                $"{KEY}-{host}",
                async _ =>
                {
                    if (host == "www.weasyl.com")
                        return "https://www.weasyl.com/img/favicon-oP29Tyisif.svg";

                    try
                    {
                        using var client = httpClientFactory.CreateClient();
                        client.Timeout = TimeSpan.FromSeconds(5);

                        using var req = new HttpRequestMessage(HttpMethod.Get, $"https://{host}");
                        req.Headers.Accept.ParseAdd("text/html");

                        using var resp = await client.SendAsync(req, cancellationToken);

                        var html = await resp
                            .EnsureSuccessStatusCode()
                            .Content
                            .ReadAsStringAsync(cancellationToken);

                        var href = ImageFinder
                            .FindFaviconsInHTML(html)
                            .Last();

                        return new Uri(req.RequestUri!, href).AbsoluteUri;
                    }
                    catch (Exception ex)
                    {
                        Console.Error.WriteLine(ex);
                        return null;
                    }
                },
                new MemoryCacheEntryOptions
                {
                    AbsoluteExpiration = DateTimeOffset.UtcNow.AddDays(1)
                });
    }
}
