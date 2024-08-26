using Microsoft.EntityFrameworkCore;
using Pandacap.Data;
using Pandacap.LowLevel.ATProto;

namespace Pandacap.HighLevel
{
    public class ATProtoCredentialProvider(PandacapDbContext context)
    {
        public class AutomaticRefreshCredentials(
            PandacapDbContext context,
            ATProtoCredentials credentials) : IAutomaticRefreshCredentials
        {
            public string DID => credentials.DID;
            public string PDS => credentials.PDS;

            public string AccessToken { get; private set; } = credentials.AccessToken;

            public string RefreshToken { get; private set; } = credentials.RefreshToken;

            public async Task UpdateTokensAsync(ITokenPair newCredentials)
            {
                AccessToken = newCredentials.AccessToken;
                RefreshToken = newCredentials.RefreshToken;

                await foreach (var dbRecord in context.ATProtoCredentials.Where(a => a.DID == credentials.DID).AsAsyncEnumerable())
                {
                    dbRecord.AccessToken = AccessToken;
                    dbRecord.RefreshToken = RefreshToken;
                    await context.SaveChangesAsync();
                }
            }
        }

        private readonly Lazy<Task<AutomaticRefreshCredentials?>> Credentials = new(async () =>
        {
            var credentials = await context.ATProtoCredentials.FirstOrDefaultAsync();
            return credentials == null
                ? null
                : new AutomaticRefreshCredentials(
                    context,
                    credentials);
        });

        public async Task<AutomaticRefreshCredentials?> GetCredentialsAsync() =>
            await Credentials.Value;
    }
}
