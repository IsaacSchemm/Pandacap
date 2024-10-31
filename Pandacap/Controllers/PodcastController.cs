using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NAudio.MediaFoundation;
using NAudio.Wave;
using System.IO.Compression;

namespace Pandacap.Controllers
{
    [Authorize]
    public class PodcastController(IHttpClientFactory httpClientFactory) : Controller
    {
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
