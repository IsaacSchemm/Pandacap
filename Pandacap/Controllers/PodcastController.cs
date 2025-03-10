﻿using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Pandacap.Podcasts;

namespace Pandacap.Controllers
{
    [Authorize]
    public class PodcastController(
        PodcastStreamProvider podcastStreamProvider,
        WmaZipSplitter wmaZipSplitter) : Controller
    {
        public async Task<IActionResult> Download(
            string url,
            CancellationToken cancellationToken = default)
        {
            var uri = new Uri(url);

            var accessor = await podcastStreamProvider.CreateAccessorAsync(
                uri,
                cancellationToken);

            var mediaType = accessor.ContentType?.MediaType ?? "application/octet-stream";

            return File(
                accessor.Stream,
                mediaType,
                fileDownloadName: uri.Segments.Last(),
                enableRangeProcessing: accessor.EnableRangeProcessing);
        }

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
