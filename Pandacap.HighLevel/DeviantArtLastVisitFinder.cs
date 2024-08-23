using DeviantArtFs.Extensions;
using DeviantArtFs.ParameterTypes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Pandacap.HighLevel
{
    public class DeviantArtLastVisitFinder(
        DeviantArtCredentialProvider deviantArtCredentialProvider)
    {
        public async Task<DateTimeOffset?> FindMyLastVisitAsync()
        {
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
                        return friend.lastvisit.OrNull();

                    if (friend.user.username.CompareTo(user.username) > 0)
                        break;
                }
            }

            return null;
        }
    }
}
