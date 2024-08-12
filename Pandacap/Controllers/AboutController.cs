using Microsoft.AspNetCore.Mvc;

namespace Pandacap.Controllers
{
    public class AboutController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
