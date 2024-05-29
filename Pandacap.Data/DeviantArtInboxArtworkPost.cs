namespace Pandacap.Data
{
    public class DeviantArtInboxThumbnail
    {
        public string Url { get; set; } = "";
        public int Width { get; set; }
        public int Height { get; set; }
    }

    public class DeviantArtInboxArtworkPost : DeviantArtInboxPost, IImage, IImagePost
    {
        public List<DeviantArtInboxThumbnail> Thumbnails { get; set; } = [];

        public DeviantArtInboxThumbnail? DefaultThumbnail =>
            Thumbnails
            .OrderBy(t => t.Height >= 150 ? 1 : 2)
            .ThenBy(t => t.Height)
            .FirstOrDefault();

        public string? ThumbnailUrl => DefaultThumbnail?.Url;

        public string? ThumbnailSrcset => Thumbnails.Skip(1).Any()
            ? string.Join(", ", Thumbnails.Select(x => $"{x.Url} {1.0 * x.Height / 150}x"))
            : null;

        string? IImage.AltText => null;

        IEnumerable<IImage> IImagePost.Images => [this];
    }
}
