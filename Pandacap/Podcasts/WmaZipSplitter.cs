using NAudio.MediaFoundation;
using NAudio.Wave;
using System.IO.Compression;

namespace Pandacap.Podcasts
{
    public class WmaZipSplitter(
        IHttpClientFactory httpClientFactory)
    {
        public async Task SegmentZip(
            Uri uri,
            TimeSpan segmentLength,
            Stream outputStream,
            CancellationToken cancellationToken)
        {
            string filename = uri.Segments.Last();
            string basename = Path.GetFileNameWithoutExtension(filename);

            string tempfile = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid()}-{filename}");

            try
            {
                MediaFoundationApi.Startup();

                using (var client = httpClientFactory.CreateClient())
                {
                    using var resp = await client.GetAsync(uri, cancellationToken);
                    using var stream = await resp.Content.ReadAsStreamAsync(cancellationToken);
                    using var fs = new FileStream(tempfile, FileMode.CreateNew, FileAccess.Write);
                    await stream.CopyToAsync(fs, cancellationToken);
                }

                using var reader = new AudioFileReader(tempfile);

                byte[] buffer = new byte[
                    reader.WaveFormat.SampleRate
                    * (long)segmentLength.TotalSeconds
                    * reader.WaveFormat.Channels
                    * reader.WaveFormat.BitsPerSample
                    / 8];

                using var archive = new ZipArchive(
                    outputStream,
                    ZipArchiveMode.Create,
                    leaveOpen: false);

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
            finally
            {
                if (File.Exists(tempfile))
                    File.Delete(tempfile);
            }
        }
    }
}
