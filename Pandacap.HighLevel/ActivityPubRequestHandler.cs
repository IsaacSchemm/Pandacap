using JsonLD.Core;
using Microsoft.Win32;
using Newtonsoft.Json.Linq;
using Pandacap.LowLevel;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;

namespace Pandacap.HighLevel
{
    /// <summary>
    /// Performs requests to other ActivityPub servers.
    /// </summary>
    public class ActivityPubRequestHandler(
        ApplicationInformation appInfo,
        IHttpClientFactory httpClientFactory,
        KeyProvider keyProvider,
        IdMapper mapper)
    {
        /// <summary>
        /// Adds an HTTP signature to the request.
        /// </summary>
        /// <param name="req">The request message to be sent</param>
        private async Task AddSignatureAsync(HttpRequestMessage req)
        {
            IEnumerable<string> toSign()
            {
                yield return $"(request-target): {req.Method.Method.ToLowerInvariant()} {req.RequestUri!.AbsolutePath}";
                yield return $"host: {req.Headers.Host}";
                yield return $"date: {req.Headers.Date:r}";
                if (req.Headers.TryGetValues("Digest", out var values))
                {
                    yield return $"digest: {values.Single()}";
                }
            }

            string ds = string.Join("\n", toSign());
            byte[] data = Encoding.UTF8.GetBytes(ds);
            byte[] signature = await keyProvider.SignRsaSha256Async(data);
            string headerNames = "(request-target) host date";
            if (req.Headers.Contains("Digest"))
            {
                headerNames += " digest";
            }
            req.Headers.Add("Signature", $"keyId=\"{mapper.ActorId}#main-key\",algorithm=\"rsa-sha256\",headers=\"{headerNames}\",signature=\"{Convert.ToBase64String(signature)}\"");
        }

        /// <summary>
        /// Makes a signed HTTP POST request to a remote ActivityPub server.
        /// </summary>
        /// <param name="url">The URL to request</param>
        /// <param name="json">The raw JSON-LD request body</param>
        public async Task PostAsync(Uri url, string json)
        {
            byte[]? body = Encoding.UTF8.GetBytes(json);
            string? digest = Convert.ToBase64String(SHA256.HashData(body));

            using var req = new HttpRequestMessage(HttpMethod.Post, url);
            req.Headers.Host = url.Host;
            req.Headers.Date = DateTime.UtcNow;
            req.Headers.UserAgent.ParseAdd(appInfo.UserAgent);

            req.Headers.Add("Digest", $"SHA-256={digest}");

            await AddSignatureAsync(req);

            req.Content = new ByteArrayContent(body);
            req.Content.Headers.ContentType = new MediaTypeHeaderValue("application/activity+json");

            using var httpClient = httpClientFactory.CreateClient();

            using var res = await httpClient.SendAsync(req);
            res.EnsureSuccessStatusCode();
        }

        /// <summary>
        /// Makes a signed HTTP GET request to a remote ActivityPub server.
        /// </summary>
        /// <param name="url">The URL to request</param>
        /// <param name="cancellationToken">A cancellation token (optional)</param>
        /// <returns>The raw JSON-LD response</returns>
        public async Task<string> GetJsonAsync(
            Uri url,
            CancellationToken cancellationToken = default)
        {
            return await GetJsonAsync(url, true, cancellationToken);
        }

        /// <summary>
        /// Makes a signed HTTP GET request to a remote ActivityPub server.
        /// </summary>
        /// <param name="url">The URL to request</param>
        /// <param name="includeSignature">Whether to include a Mastodon-style HTTP signature</param>
        /// <param name="cancellationToken">A cancellation token (optional)</param>
        /// <returns>The raw JSON-LD response</returns>
        public async Task<string> GetJsonAsync(
            Uri url,
            bool includeSignature,
            CancellationToken cancellationToken = default)
        {
            try
            {
                using var req = new HttpRequestMessage(HttpMethod.Get, url);
                req.Headers.Host = url.Host;
                req.Headers.Date = DateTime.UtcNow;
                req.Headers.UserAgent.ParseAdd(appInfo.UserAgent);

                if (includeSignature)
                    await AddSignatureAsync(req);

                req.Headers.Accept.Add(MediaTypeWithQualityHeaderValue.Parse("application/ld+json; profile=\"https://www.w3.org/ns/activitystreams\""));
                req.Headers.Accept.Add(MediaTypeWithQualityHeaderValue.Parse("application/activity+json"));

                using var httpClient = httpClientFactory.CreateClient();

                using var res = await httpClient.SendAsync(req, cancellationToken);
                res.EnsureSuccessStatusCode();

                if (res.Content.Headers.ContentType?.MediaType == "text/html")
                {
                    string html = await res.Content.ReadAsStringAsync(cancellationToken);
                    var links = LinkRelAlternate.ParseFromHtml(html)
                        .Where(attr => attr.Type == "application/activity+json")
                        .Select(attr => attr.Href);
                    string? href = links.FirstOrDefault()
                        ?? throw new Exception("Request returned an HTML response with no link rel=alternate for application/activity+json");

                    if (href == url.OriginalString)
                        throw new Exception("Detected circular link rel=alternate reference");

                    return await GetJsonAsync(
                        new Uri(href),
                        cancellationToken);
                }

                return await res.Content.ReadAsStringAsync(cancellationToken);
            }
            catch (Exception) when (includeSignature)
            {
                return await GetJsonAsync(
                    url,
                    includeSignature: false,
                    cancellationToken);
            }
        }
    }
}
