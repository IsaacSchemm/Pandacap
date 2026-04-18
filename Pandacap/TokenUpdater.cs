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
            if (info.LoginProvider != "DeviantArt")
                return;

            if (info.Principal.Identity?.Name is not string name)
                return;

            var authenticationTokens = info.AuthenticationTokens
                ?? throw new Exception("No AuthenticationTokens found.");

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
