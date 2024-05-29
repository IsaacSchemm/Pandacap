using JsonLD.Core;
using Newtonsoft.Json.Linq;
using Pandacap.LowLevel;
using System.Net.Http.Headers;
using System.Text;

namespace Pandacap.HighLevel.ActivityPub
{
    /// <summary>
    /// Performs requests to other ActivityPub servers.
    /// </summary>
    public class RemoteActorFetcher(
        ApplicationInformation appInfo,
        IHttpClientFactory httpClientFactory,
        KeyProvider keyProvider,
        IdMapper mapper)
    {
        /// <summary>
        /// Fetches and returns an actor.
        /// </summary>
        /// <param name="url">The actor ID / URL</param>
        /// <returns>An actor record</returns>
        public async Task<RemoteActor> FetchActorAsync(string url)
        {
            string json = await GetJsonAsync(new Uri(url));

            JObject document = JObject.Parse(json);
            JArray expansion = JsonLdProcessor.Expand(document);

            string id = expansion[0]["@id"].Value<string>();

            string inbox = expansion[0]["http://www.w3.org/ns/ldp#inbox"][0]["@id"].Value<string>();

            string? sharedInbox = null;
            foreach (var endpoint in expansion[0]["https://www.w3.org/ns/activitystreams#endpoints"] ?? Enumerable.Empty<JToken>())
            {
                foreach (var si in endpoint["https://www.w3.org/ns/activitystreams#sharedInbox"])
                {
                    sharedInbox = si["@id"].Value<string>();
                }
            }

            string? preferredUsername = expansion[0]["https://www.w3.org/ns/activitystreams#preferredUsername"]
                .Select(token => token["@value"].Value<string>())
                .FirstOrDefault();
            string? iconUrl = expansion[0]["https://www.w3.org/ns/activitystreams#icon"]
                .SelectMany(token => token["https://www.w3.org/ns/activitystreams#url"])
                .Select(token => token["@id"].Value<string>())
                .FirstOrDefault();

            string keyId = expansion[0]["https://w3id.org/security#publicKey"][0]["@id"].Value<string>();
            string keyPem = expansion[0]["https://w3id.org/security#publicKey"][0]["https://w3id.org/security#publicKeyPem"][0]["@value"].Value<string>();

            return new RemoteActor(
                Id: id,
                Inbox: inbox,
                SharedInbox: sharedInbox,
                PreferredUsername: preferredUsername,
                IconUrl: iconUrl,
                KeyId: keyId,
                KeyPem: keyPem);
        }

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
        /// Makes a signed HTTP GET request to a remote ActivityPub server.
        /// </summary>
        /// <param name="url">The URL to request</param>
        /// <returns>The raw JSON-LD response</returns>
        public async Task<string> GetJsonAsync(Uri url)
        {
            using var req = new HttpRequestMessage(HttpMethod.Get, url);
            req.Headers.Host = url.Host;
            req.Headers.Date = DateTime.UtcNow;
            req.Headers.UserAgent.ParseAdd(appInfo.UserAgent);

            await AddSignatureAsync(req);

            req.Headers.Accept.Add(MediaTypeWithQualityHeaderValue.Parse("application/ld+json; profile=\"https://www.w3.org/ns/activitystreams\""));
            req.Headers.Accept.Add(MediaTypeWithQualityHeaderValue.Parse("application/activity+json"));

            using var httpClient = httpClientFactory.CreateClient();

            using var res = await httpClient.SendAsync(req);
            res.EnsureSuccessStatusCode();

            return await res.Content.ReadAsStringAsync();
        }
    }
}
