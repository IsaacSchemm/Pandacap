using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Pandacap.Data;

namespace Pandacap.Controllers
{
    public class GeneralPostsController(PandacapDbContext context) : Controller
    {
        public async Task<IActionResult> Index(Guid id, CancellationToken cancellationToken)
        {
            var feedItem = await context.GeneralInboxItems
                .Where(i => i.Id == id)
                .FirstOrDefaultAsync(cancellationToken);

            return feedItem == null
                ? NotFound()
                : View(feedItem);
        }
    }
}
