using Pandacap.Data;

namespace Pandacap.HighLevel
{
    public static class ThumbnailExtensions
    {
        public static string? GetThumbnailSrcset(this IPostImage image, double height) =>
            image.Thumbnails.Any()
                ? string.Join(", ", image.Thumbnails.Select(x => $"{x.Url} {x.Height / height}x"))
                : null;
    }
}
