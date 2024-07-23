using Microsoft.AspNetCore.Mvc;
using NAudio.MediaFoundation;
using NAudio.Wave;
using System.IO.Compression;

namespace Pandacap.Controllers
{
    public class PodcastController(IHttpClientFactory httpClientFactory) : Controller
    {
        private class WaveProvider(Stream stream, WaveFormat waveFormat) : IWaveProvider
        {
            public WaveFormat WaveFormat => waveFormat;

            public int Read(byte[] buffer, int offset, int count) =>
                stream.Read(buffer, offset, count);
        }

        public class LengthStream : Stream
        {
            public override bool CanRead => false;

            public override bool CanSeek => false;

            public override bool CanWrite => true;

            public override long Length => throw new NotImplementedException();

            public override long Position { get; set; }

            public override void Flush() { }

            public override int Read(byte[] buffer, int offset, int count)
            {
                throw new NotImplementedException();
            }

            public override long Seek(long offset, SeekOrigin origin)
            {
                throw new NotImplementedException();
            }

            public override void SetLength(long value)
            {
                throw new NotImplementedException();
            }

            public override void Write(byte[] buffer, int offset, int count)
            {
                Position += count;
            }
        }

        public async Task<IActionResult> FiveMinuteSegmentsZip(string url, CancellationToken cancellationToken)
        {
            MediaFoundationApi.Startup();

            using var client = httpClientFactory.CreateClient();
            using var resp = await client.GetAsync(url, cancellationToken);

            if (resp.Content.Headers.ContentType?.MediaType != "audio/mpeg")
                throw new NotImplementedException();

            using var stream = await resp.Content.ReadAsStreamAsync(cancellationToken);

            using var reader = new Mp3FileReader(stream);

            byte[] buffer = new byte[2 * 44100 * 60 * 5 * reader.WaveFormat.Channels];

            Response.ContentType = "application/zip";
            Response.Headers["Content-Disposition"] = $"attachment;filename=podcast_{DateTime.UtcNow:yyyyMMdd-hhmmss}.zip";

            int i = 0;
            using (var s = Response.BodyWriter.AsStream())
            {
                using var archive = new ZipArchive(s, ZipArchiveMode.Create, true);
                while (true)
                {
                    int read = await reader.ReadAsync(buffer, cancellationToken);

                    using var ms1 = new MemoryStream(buffer, 0, read);
                    using var ms2 = new MemoryStream();

                    if (read == 0)
                        break;

                    var x = MediaFoundationEncoder.GetEncodeBitrates(AudioSubtypes.MFAudioFormat_WMAudioV9, 44100, 1);

                    MediaFoundationEncoder.EncodeToWma(
                        new WaveProvider(
                            ms1,
                            reader.WaveFormat),
                        ms2);

                    var entry = archive.CreateEntry($"podcast_{++i:000}.wma", CompressionLevel.NoCompression);
                    using var zipStream = entry.Open();
                    await zipStream.WriteAsync(ms2.ToArray(), cancellationToken);
                }
            }

            return new EmptyResult();
        }
    }
}
