using Pandacap.Clients.ATProto;
using Pandacap.ConfigurationObjects;

namespace Pandacap
{
    public class BridgyFedDIDProvider(
        ApplicationInformation appInfo,
        IHttpClientFactory httpClientFactory)
    {
        public async Task<string?> GetDIDAsync()
        {
            try
            {
                using var client = httpClientFactory.CreateClient();
                client.DefaultRequestHeaders.UserAgent.ParseAdd(UserAgentInformation.UserAgent);

                var handle = $"{appInfo.Username}.{appInfo.ApplicationHostname}.ap.brid.gy";

                var repo = await XRPC.Com.Atproto.Repo.DescribeRepoAsync(
                    client,
                    XRPC.Host.Unauthenticated("atproto.brid.gy"),
                    handle);

                return repo.did;
            }
            catch (Exception)
            {
                return null;
            }
        }
    }
}
