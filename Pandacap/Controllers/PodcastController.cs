using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Pandacap.ConfigurationObjects;
using Pandacap.Data;
using Pandacap.Podcasts;
using System.Net;

namespace Pandacap.Controllers
{
    [Authorize]
    public class PodcastController(
        PandacapDbContext context,
        IHttpClientFactory httpClientFactory,
        WmaZipSplitter wmaZipSplitter) : Controller
    {
        public async Task<IActionResult> Player(Guid id)
        {
            var feedItem = await context.RssFeedItems
                .Where(i => i.Id == id)
                .SingleAsync();

            return View(feedItem);
        }

        public async Task<IActionResult> GetContentType(
            string url,
            CancellationToken cancellationToken)
        {
            var client = httpClientFactory.CreateClient();
            client.DefaultRequestHeaders.UserAgent.ParseAdd(UserAgentInformation.UserAgent);

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
            client.DefaultRequestHeaders.UserAgent.ParseAdd(UserAgentInformation.UserAgent);

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
