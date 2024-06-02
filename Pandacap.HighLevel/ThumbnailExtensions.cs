using Pandacap.Data;

namespace Pandacap.HighLevel
{
    public static class ThumbnailExtensions
    {
        public static string? GetThumbnailSrc(this IThumbnail thumbnail) => thumbnail.Renditions
            .OrderBy(t => t.Height >= 150 ? 1 : 2)
            .ThenBy(t => t.Height)
            .Select(t => t.Url)
            .FirstOrDefault();

        public static string? GetThumbnailSrcset(this IThumbnail thumbnail) => thumbnail.Renditions.Skip(1).Any()
            ? string.Join(", ", thumbnail.Renditions.Select(x => $"{x.Url} {1.0 * x.Height / 150}x"))
            : null;
    }
}
