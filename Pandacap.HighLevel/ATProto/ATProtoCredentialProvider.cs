using Microsoft.EntityFrameworkCore;
using Pandacap.Clients.ATProto;
using Pandacap.Data;

namespace Pandacap.HighLevel.ATProto
{
    public class ATProtoCredentialProvider(PandacapDbContext context)
    {
        public class AutomaticRefreshCredentials(
            PandacapDbContext context,
            ATProtoCredentials credentials) : XRPC.IRefreshCredentials
        {
            public string DID => credentials.DID;
            public string PDS => credentials.PDS;

            public bool CrosspostTarget => credentials.CrosspostTargetSince != null;

            public string AccessToken { get; private set; } = credentials.AccessToken;
            public string RefreshToken { get; private set; } = credentials.RefreshToken;
            public string Handle { get; private set; } = credentials.RefreshToken;

            public async Task UpdateTokensAsync(ATProtoTokens tokens)
            {
                AccessToken = tokens.AccessToken;
                RefreshToken = tokens.RefreshToken;
                Handle = tokens.Handle;

                await foreach (var dbRecord in context.ATProtoCredentials.Where(a => a.DID == credentials.DID).AsAsyncEnumerable())
                {
                    dbRecord.AccessToken = AccessToken;
                    dbRecord.RefreshToken = RefreshToken;
                    dbRecord.Handle = Handle;
                    await context.SaveChangesAsync();
                }
            }
        }

        private readonly Lazy<Task<IReadOnlyList<AutomaticRefreshCredentials>>> AllCredentials = new(async () =>
        {
            var credentials = await context.ATProtoCredentials.ToListAsync();

            return [.. credentials.Select(c => new AutomaticRefreshCredentials(context, c))];
        });

        public async Task<IReadOnlyList<AutomaticRefreshCredentials>> GetAllCredentialsAsync()
        {
            return await AllCredentials.Value;
        }

        public async Task<AutomaticRefreshCredentials?> GetCredentialsAsync(string did)
        {
            var credentials = await AllCredentials.Value;
            return credentials
                .Where(c => c.DID == did)
                .FirstOrDefault();
        }

        public async Task<AutomaticRefreshCredentials?> GetCrosspostingCredentialsAsync()
        {
            var credentials = await AllCredentials.Value;
            return credentials
                .Where(c => c.CrosspostTarget)
                .FirstOrDefault();
        }
    }
}
