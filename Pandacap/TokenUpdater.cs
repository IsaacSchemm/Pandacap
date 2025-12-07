using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Pandacap.Data;

namespace Pandacap
{
    public class TokenUpdater(
        AllowedExternalUserCollection allowedExternalUserCollection,
        PandacapDbContext context)
    {
        public async Task UpdateTokensAsync(ExternalLoginInfo info)
        {
            var authenticationTokens = info.AuthenticationTokens
                ?? throw new Exception("No AuthenticationTokens found.");

            if (info.LoginProvider == "DeviantArt")
            {
                if (info.Principal.Identity?.Name is not string name
                    || !allowedExternalUserCollection.DeviantArtUsers.Contains(name))
                {
                    throw new Exception($"This user is not allowed to log in via {info.LoginProvider}.");
                }

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
            else if (info.LoginProvider == "Reddit")
            {
                if (info.Principal.Identity?.Name is not string name
                    || !allowedExternalUserCollection.DeviantArtUsers.Contains(name))
                {
                    throw new Exception($"This user is not allowed to log in via {info.LoginProvider}.");
                }

                var credentials = await context.RedditCredentials
                    .Where(c => c.Username == info.Principal.Identity.Name)
                    .SingleOrDefaultAsync();

                if (credentials == null)
                {
                    credentials = new RedditCredentials
                    {
                        Username = info.Principal.Identity.Name
                    };
                    context.RedditCredentials.Add(credentials);
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
