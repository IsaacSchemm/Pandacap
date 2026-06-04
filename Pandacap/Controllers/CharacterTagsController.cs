using Microsoft.AspNetCore.Mvc;

namespace Pandacap.Controllers
{
    [Obsolete("Use CanonicalTagsController.Character")]
    public class CharacterTagsController : Controller
    {
        public IActionResult Index(Guid id, CancellationToken cancellationToken) =>
            RedirectToAction("Character", "CanonicalTags", new { id });
    }
}
