using Pandacap.Clients.ATProto;
using Pandacap.ConfigurationObjects;

namespace Pandacap
{
    public class BridgyFedHandleProvider(
        ApplicationInformation appInfo,
        IHttpClientFactory httpClientFactory)
    {
        private record Info(string Handle, string DID);

        private async Task<Info?> FindAsync()
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

                return new(repo.handle, repo.did);
            }
            catch (Exception)
            {
                return null;
            }
        }

        public async Task<string?> GetHandleAsync()
        {
            var info = await FindAsync();
            return info?.Handle;
        }

        public async Task<string?> GetDIDAsync()
        {
            var info = await FindAsync();
            return info?.DID;
        }
    }
}
