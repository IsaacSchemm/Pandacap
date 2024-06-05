using DeviantArtFs;
using DeviantArtFs.Extensions;
using DeviantArtFs.ParameterTypes;
using DeviantArtFs.ResponseTypes;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Pandacap.Data;
using Pandacap.HighLevel;
using Pandacap.Models;

namespace Pandacap.Controllers
{
    public class FavoritesController(
        PandacapDbContext context,
        DeviantArtFeedReader feedReader) : Controller
    {
        public async Task<IActionResult> Index(string? next, int? count)
        {
            var activityPubImagePosts = context.RemoteActivityPubImagePosts
                .Where(post => post.FavoritedAt != null)
                .AsAsyncEnumerable();

            var activityPubTextPosts = context.RemoteActivityPubTextPosts
                .Where(post => post.FavoritedAt != null)
                .AsAsyncEnumerable();

            var deviantArtPosts = feedReader.GetFavoriteDeviationsAsync();

            var posts =
                await new[]
                {
                    activityPubImagePosts,
                    activityPubTextPosts,
                    deviantArtPosts
                }
                .MergeNewest(post => post.Timestamp)
                .SkipUntil(post => post.Id == next || next == null)
                .AsListPage(count ?? 20);

            return View("List", new ListViewModel<IPost>
            {
                Title = "Favorites",
                Items = posts
            });
        }
    }
}
