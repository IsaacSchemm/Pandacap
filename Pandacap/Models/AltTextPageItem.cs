using DeviantArtFs.Extensions;
using DeviantArtFs.ResponseTypes;
using Pandacap.Data;

namespace Pandacap.Models
{
    public record AltTextPageItem(
        Deviation Deviation,
        string? AltText) : IThumbnail
    {
        private record Thumb(Preview Preview) : IThumbnailRendition
        {
            string? IThumbnailRendition.Url => Preview.src;
            int IThumbnailRendition.Width => Preview.width;
            int IThumbnailRendition.Height => Preview.height;
        }

        IEnumerable<IThumbnailRendition> IThumbnail.Renditions =>
            Deviation.thumbs.OrEmpty().Select(t => new Thumb(t));
    };
}
