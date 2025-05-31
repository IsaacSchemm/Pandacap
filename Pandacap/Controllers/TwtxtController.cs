using Microsoft.AspNetCore.Mvc;

namespace Pandacap.Controllers
{
    [Obsolete]
    public class TwtxtController : Controller
    {
        [Route("twtxt.txt")]
        public IActionResult Index()
        {
            return Redirect("/Gallery/Composite?format=twtxt");
        }
    }
}
