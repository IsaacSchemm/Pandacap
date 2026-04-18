using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Pandacap.Database;

namespace Pandacap
{
    public class TokenUpdater(
        PandacapDbContext context)
    {
        public async Task UpdateTokensAsync(ExternalLoginInfo info)
        {
            var authenticationTokens = info.AuthenticationTokens
                ?? throw new Exception("No AuthenticationTokens found.");

            if (info.LoginProvider == "DeviantArt"
                && info.Principal.Identity?.Name is string name)
            {
                var credentials = await context.DeviantArtCredentials
                    .Where(c => c.Username == info.Principal.Identity.Name)
                    .SingleOrDefaultAsync();

                if (credentials == null)
                {
                    credentials = new DeviantArtCredentials
                    {
                        Username = info.Principal.Identity.Name
                    };
                    context.DeviantArtCredentials.Add(credentials);
                }

                credentials.Username = info.Principal.Identity.Name;
                credentials.AccessToken = authenticationTokens
                    .Where(t => t.Name == "access_token")
                    .Select(t => t.Value)
                    .Single();
                credentials.RefreshToken = authenticationTokens
                    .Where(t => t.Name == "refresh_token")
                    .Select(t => t.Value)
                    .Single();

                await context.SaveChangesAsync();
            }
        }
    }
}
