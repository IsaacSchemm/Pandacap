﻿namespace Pandacap.Data
{
    public class DeviantArtOurArtworkPost : DeviantArtOurPost, IThumbnail, IImagePost
    {
        public DeviantArtOurImage Image { get; set; } = new DeviantArtOurImage();

        public List<DeviantArtOurImage> Thumbnails { get; set; } = [];

        public DeviantArtOurImage? DefaultThumbnail =>
            Thumbnails
            .OrderBy(t => t.Height >= 150 ? 1 : 2)
            .ThenBy(t => t.Height)
            .FirstOrDefault();

        public string? ThumbnailUrl => DefaultThumbnail?.Url;

        public string? ThumbnailSrcset => Thumbnails.Skip(1).Any()
            ? string.Join(", ", Thumbnails.Select(x => $"{x.Url} {1.0 * x.Height / 150}x"))
            : null;

        public string? AltText { get; set; }

        IEnumerable<IThumbnail> IImagePost.Thumbnails => [this];
    }
}
