using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Pandacap.Podcasts;

namespace Pandacap.Controllers
{
    [Authorize]
    public class PodcastController(
        WmaZipSplitter wmaZipSplitter) : Controller
    {
        public IActionResult Player(string url)
        {
            ViewBag.Src = url;

            return View();
        }

        public async Task<IActionResult> SegmentZip(
            string url,
            int seconds,
            CancellationToken cancellationToken)
        {
            Response.ContentType = "application/zip";
            Response.Headers.ContentDisposition = $"attachment;filename=podcast.zip";

            await wmaZipSplitter.SegmentZip(
                new Uri(url),
                TimeSpan.FromSeconds(seconds),
                Response.BodyWriter.AsStream(),
                cancellationToken);

            return new EmptyResult();
        }
    }
}
