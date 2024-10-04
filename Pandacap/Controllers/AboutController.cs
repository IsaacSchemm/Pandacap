using Microsoft.AspNetCore.Mvc;
using Pandacap.HighLevel;

namespace Pandacap.Controllers
{
    public class AboutController(ATProtoDIDResolver aTProtoDIDResolver) : Controller
    {
        public async Task<IActionResult> Index(string? did = null)
        {
            if (did is string d)
            {
                var found = await aTProtoDIDResolver.GetPDSAsync(d);
                return Json(found);
            }

            return View();
        }
    }
}
