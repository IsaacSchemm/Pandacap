using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Pandacap.Audio.Interfaces;
using System.Net;

namespace Pandacap.Controllers
{
    [Authorize]
    public class PodcastController(
        IAudioSplitter audioSplitter,
        IHttpClientFactory httpClientFactory) : Controller
    {
        public async Task<IActionResult> GetContentType(
            string url,
            CancellationToken cancellationToken)
        {
            var client = httpClientFactory.CreateClient();

            using var req = new HttpRequestMessage(HttpMethod.Head, url);
            using var resp = await client.SendAsync(req, cancellationToken);

            if (!resp.IsSuccessStatusCode)
                return StatusCode((int)HttpStatusCode.BadGateway);

            return Ok(
                resp.EnsureSuccessStatusCode().Content.Headers.ContentType?.MediaType
                ?? "application/octet-stream");
        }

        public async Task<IActionResult> Download(
            string url,
            CancellationToken cancellationToken)
        {
            var client = httpClientFactory.CreateClient();

            var req = new HttpRequestMessage(HttpMethod.Get, url);
            var resp = await client.SendAsync(req, HttpCompletionOption.ResponseHeadersRead, cancellationToken);

            if (!resp.IsSuccessStatusCode)
                return StatusCode((int)HttpStatusCode.BadGateway);

            var uri = resp.Content.Headers.ContentLocation
                ?? req.RequestUri
                ?? new Uri("https://www.example.com/file");

            return File(
                await resp.Content.ReadAsStreamAsync(cancellationToken),
                resp.Content.Headers.ContentType?.MediaType ?? "application/octet-stream",
                uri.AbsolutePath.Split('/').Last());
        }

        public async Task<IActionResult> SegmentZip(
            string url,
            CancellationToken cancellationToken)
        {
            Response.ContentType = "application/zip";
            Response.Headers.ContentDisposition = $"attachment;filename=podcast.zip";

            await audioSplitter.SplitIntoSegmentsAsync(
                new Uri(url),
                TimeSpan.FromMinutes(5),
                AudioSplitterOutputAudioFormat.WMA,
                AudioSplitterOutputArchiveFormat.ZIP,
                Response.BodyWriter.AsStream(),
                cancellationToken);

            return new EmptyResult();
        }
    }
}
