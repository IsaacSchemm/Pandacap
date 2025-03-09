using System.Net.Http.Headers;

namespace Pandacap.Podcasts
{
    internal class PodcastStream(
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
            throw new NotImplementedException();

        public override async ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
        {
            long to = Math.Min(
                Position + buffer.Length,
                Length);

            if (Position == to)
                return 0;

            using var client = httpClientFactory.CreateClient();

            using var req = new HttpRequestMessage(HttpMethod.Get, Uri);
            req.Headers.Range = new RangeHeaderValue(Position, to);

            using var resp = await client.SendAsync(req, cancellationToken);
            resp.EnsureSuccessStatusCode();

            using var stream = await resp.Content.ReadAsStreamAsync(cancellationToken);

            int read = await stream.ReadAsync(buffer, cancellationToken);
            Position += read;

            return read;
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            return Position = origin switch
            {
                SeekOrigin.Begin => offset,
                SeekOrigin.Current => Position + offset,
                SeekOrigin.End => ContentLength + offset,
                _ => throw new ArgumentException($"Unrecgonized origin {origin}", nameof(origin))
            };
        }

        public override void SetLength(long value) =>
            throw new NotImplementedException();

        public override void Write(byte[] buffer, int offset, int count) =>
            throw new NotImplementedException();
    }
}
