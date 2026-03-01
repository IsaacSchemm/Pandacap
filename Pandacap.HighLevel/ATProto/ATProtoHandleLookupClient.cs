using DnsClient;
using DnsClient.Protocol;
using Pandacap.Clients.ATProto;

namespace Pandacap.HighLevel.ATProto
{
    public class ATProtoHandleLookupClient(
        DIDResolver didResolver,
        IHttpClientFactory httpClientFactory,
        ILookupClient lookupClient)
    {
        private async Task<string?> FindDIDWithDNSAsync(
            string handle,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var queryResponse = await lookupClient.QueryAsync(
                    $"_atproto.{handle}",
                    QueryType.TXT,
                    cancellationToken: cancellationToken);

                foreach (var answer in queryResponse.Answers)
                    if (answer is TxtRecord txt)
                        foreach (var value in txt.Text)
                            if (value.StartsWith("did=did:"))
                                return value[4..];
            }
            catch (Exception) { }

            return null;
        }

        private async Task<string?> FindDIDWithWellKnownAsync(
            string handle,
            CancellationToken cancellationToken = default)
        {
            using var client = httpClientFactory.CreateClient();

            try
            {
                using var resp = await client.GetAsync(
                    $"https://{handle}/.well-known/atproto-did",
                    cancellationToken);

                var text = await resp.EnsureSuccessStatusCode().Content.ReadAsStringAsync(cancellationToken);
                text = text.Trim();

                if (text.StartsWith("did:"))
                    return text;
            }
            catch (Exception) { }

            return null;
        }

        public async Task<string> FindDIDAsync(
            string handle,
            CancellationToken cancellationToken = default)
        {
            if (handle.StartsWith("did:"))
                return handle;

            var tokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

            List<Task<string?>> tasks = [
                FindDIDWithDNSAsync(handle, tokenSource.Token),
                FindDIDWithWellKnownAsync(handle, tokenSource.Token)
            ];

            while (tasks.Count > 0)
            {
                var task = await Task.WhenAny(tasks);
                tasks.Remove(task);

                if (await task is string did)
                {
                    var doc = await didResolver.ResolveAsync(did);
                    if (doc.Handle != handle)
                        continue;

                    tokenSource.Cancel();
                    return did;
                }
            }

            throw new Exception($"Could not resolve DID for handle: {handle}");
        }
    }
}
