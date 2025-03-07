using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NAudio.MediaFoundation;
using NAudio.Wave;
using Pandacap.Data;
using System.IO.Compression;
using System.Net.Http.Headers;

namespace Pandacap.Controllers
{
    [Authorize]
    public class PodcastController(
        PandacapDbContext context,
        IHttpClientFactory httpClientFactory) : Controller
    {
        private class HttpStream(
            IHttpClientFactory httpClientFactory,
            Uri Uri,
            long ContentLength) : Stream
        {
            public override bool CanRead => true;

            public override bool CanSeek => true;

            public override bool CanWrite => false;

            public override long Length => ContentLength;

            public override long Position { get; set; }

            public override void Flush() { }

            public override int Read(byte[] buffer, int offset, int count) =>
                ReadAsync(buffer.AsMemory(offset, count))
                .AsTask()
                .GetAwaiter()
                .GetResult();

            public override async ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
            {
                using var client = httpClientFactory.CreateClient();
                using var req = new HttpRequestMessage(HttpMethod.Get, Uri);
                req.Headers.Range = new RangeHeaderValue(Position, Position + buffer.Length);
                using var resp = await client.SendAsync(req, cancellationToken);
                resp.EnsureSuccessStatusCode();
                using var stream = await resp.Content.ReadAsStreamAsync(cancellationToken);
                int read = await stream.ReadAsync(buffer, cancellationToken);
                Position += read;
                return read;
            }

            public override long Seek(long offset, SeekOrigin origin)
            {
                return Position = origin == SeekOrigin.Begin ? offset
                    : origin == SeekOrigin.Current ? Position + offset
                    : origin == SeekOrigin.End ? ContentLength + offset
                    : throw new ArgumentException("Unrecgonized origin", nameof(origin));
            }

            public override void SetLength(long value)
            {
                throw new NotImplementedException();
            }

            public override void Write(byte[] buffer, int offset, int count)
            {
                throw new NotImplementedException();
            }
        }

        public async Task<IActionResult> ProxyAttachment(
            Guid id,
            int index = 0)
        {
            var feed = await context.RssFeedItems
                .Where(i => i.Id == id)
                .Select(i => new { i.AudioFiles })
                .SingleOrDefaultAsync();

            var audioFile = (feed?.AudioFiles ?? [])
                .Skip(index)
                .FirstOrDefault();

            if (audioFile == null)
                return NotFound();

            var uri = new Uri(audioFile.Url);

            using var req = new HttpRequestMessage(HttpMethod.Head, uri);

            using var client = httpClientFactory.CreateClient();
            using var resp = await client.SendAsync(req);

            resp.EnsureSuccessStatusCode();

            return File(
                new BufferedStream(
                    new HttpStream(
                        httpClientFactory,
                        uri,
                        resp.Content.Headers.ContentLength ?? throw new NotImplementedException()),
                    1024 * 1024 * 10),
                resp.Content.Headers.ContentType?.MediaType ?? "application/octet-stream",
                enableRangeProcessing: true);
        }

        private class WaveProvider(Stream stream, WaveFormat waveFormat) : IWaveProvider
        {
            public WaveFormat WaveFormat => waveFormat;

            public int Read(byte[] buffer, int offset, int count) =>
                stream.Read(buffer, offset, count);
        }

        public async Task<IActionResult> SegmentZip(string url, int seconds, CancellationToken cancellationToken)
        {
            string filename = new Uri(url).Segments.Last();
            string basename = Path.GetFileNameWithoutExtension(filename);

            string tempfile = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid()}-{filename}");

            try
            {
                MediaFoundationApi.Startup();

                using (var client = httpClientFactory.CreateClient())
                {
                    using var resp = await client.GetAsync(url, cancellationToken);
                    using var stream = await resp.Content.ReadAsStreamAsync(cancellationToken);
                    using var fs = new FileStream(tempfile, FileMode.CreateNew, FileAccess.Write);
                    await stream.CopyToAsync(fs, cancellationToken);
                }

                using var reader = new AudioFileReader(tempfile);

                byte[] buffer = new byte[
                    reader.WaveFormat.SampleRate
                    * seconds
                    * reader.WaveFormat.Channels
                    * reader.WaveFormat.BitsPerSample
                    / 8];

                Response.ContentType = "application/zip";
                Response.Headers.ContentDisposition = $"attachment;filename={basename}.zip";

                using (var archive = new ZipArchive(
                    Response.BodyWriter.AsStream(),
                    ZipArchiveMode.Create,
                    false))
                {
                    int i = 0;
                    while (true)
                    {
                        int read = await reader.ReadAsync(buffer, cancellationToken);
                        if (read == 0)
                            break;

                        using var inputStream = new MemoryStream(buffer, 0, read, writable: false);
                        using var outputBuffer = new MemoryStream();

                        MediaFoundationEncoder.EncodeToWma(
                            new WaveProvider(
                                inputStream,
                                reader.WaveFormat),
                            outputBuffer);

                        var entry = archive.CreateEntry($"{basename}_{++i:00}.wma", CompressionLevel.NoCompression);
                        using var zipStream = entry.Open();

                        outputBuffer.Position = 0;
                        await outputBuffer.CopyToAsync(zipStream, cancellationToken);
                    }
                }

                return new EmptyResult();
            }
            finally
            {
                if (System.IO.File.Exists(tempfile))
                    System.IO.File.Delete(tempfile);
            }
        }
    }
}
