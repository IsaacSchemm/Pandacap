using Microsoft.FSharp.Collections;
using System.Net.Http.Json;

namespace Pandacap.HighLevel
{
    public record Service(
        string Id,
        string Type,
        string ServiceEndpoint);

    public record DID(
        FSharpList<Service> Service);

    public class ATProtoDIDResolver(IHttpClientFactory httpClientFactory)
    {
        public async IAsyncEnumerable<Uri> GetPDSesAsync(string did)
        {
            var client = httpClientFactory.CreateClient();

            using var resp = await client.GetAsync($"https://plc.directory/{Uri.EscapeDataString(did)}");

            resp.EnsureSuccessStatusCode();

            var obj = await resp.Content.ReadFromJsonAsync<DID>();

            if (obj == null)
                yield break;
            if (obj.Service == null)
                yield break;

            foreach (var service in obj.Service)
                if (service.Type == "AtprotoPersonalDataServer")
                    if (Uri.TryCreate(service.ServiceEndpoint, UriKind.Absolute, out Uri? uri))
                        yield return uri;
        }
    }
}
