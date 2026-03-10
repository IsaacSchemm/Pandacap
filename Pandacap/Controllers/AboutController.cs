using Microsoft.AspNetCore.Mvc;

namespace Pandacap.Controllers
{
    public class AboutController() : Controller
    {
        public IActionResult Index()
        {
            return Redirect("https://github.com/IsaacSchemm/Pandacap/blob/main/README.md");
        }
    }
}
