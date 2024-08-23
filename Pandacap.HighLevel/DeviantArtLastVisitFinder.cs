using DeviantArtFs.Extensions;
using DeviantArtFs.ParameterTypes;
using Microsoft.Extensions.Caching.Memory;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Pandacap.HighLevel
{
    public class DeviantArtLastVisitFinder(
        DeviantArtCredentialProvider deviantArtCredentialProvider,
        IMemoryCache memoryCache)
    {
        private const string CacheKey = "c208dac9-09a3-49d2-9c15-e32ed90e1c50";

        public async Task<DateTimeOffset?> FindMyLastVisitAsync()
        {
            if (memoryCache.TryGetValue(CacheKey, out object? obj) && obj is DateTimeOffset dt)
                return dt;

            if (await deviantArtCredentialProvider.GetCredentialsAsync() is not (var credentials, var user))
                return null;

            var watchers = DeviantArtFs.Api.User.GetWatchersAsync(
                credentials,
                UserScope.ForCurrentUser,
                PagingLimit.NewPagingLimit(20),
                PagingOffset.StartingOffset);

            await foreach (var watcher in watchers)
            {
                var firstPage = await DeviantArtFs.Api.User.PageFriendsAsync(
                    credentials,
                    UserScope.NewForUser(watcher.user.username),
                    PagingLimit.NewPagingLimit(50),
                    PagingOffset.StartingOffset);

                foreach (var friend in firstPage.results.OrEmpty()) {
                    if (friend.user.username == user.username)
                        return memoryCache.Set(
                            CacheKey,
                            friend.lastvisit.OrNull(),
                            DateTimeOffset.UtcNow.AddMinutes(5));

                    if (friend.user.username.CompareTo(user.username) > 0)
                        break;
                }
            }

            return null;
        }
    }
}
