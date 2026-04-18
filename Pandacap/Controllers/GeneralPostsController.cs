using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Pandacap.Database;

namespace Pandacap.Controllers
{
    [Authorize]
    public class GeneralPostsController(PandacapDbContext pandacapDbContext) : Controller
    {
        public async Task<IActionResult> Index(Guid id, CancellationToken cancellationToken)
        {
            var feedItem = await pandacapDbContext.GeneralInboxItems
                .Where(i => i.Id == id)
                .FirstOrDefaultAsync(cancellationToken);

            return feedItem == null
                ? NotFound()
                : View(feedItem);
        }
    }
}
