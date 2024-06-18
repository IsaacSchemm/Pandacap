using DeviantArtFs.Extensions;
using DeviantArtFs.ResponseTypes;
using Pandacap.Data;

namespace Pandacap.Models
{
    public record AltTextPageItem(
        Deviation Deviation,
        string? AltText) : IPostImage
    {
        private record Thumb(Preview Preview) : IThumbnailRendition
        {
            string? IThumbnailRendition.Url => Preview.src;
            int IThumbnailRendition.Width => Preview.width;
            int IThumbnailRendition.Height => Preview.height;
        }

        public string? Url => Deviation.content.OrNull()?.src;

        public IEnumerable<IThumbnailRendition> Thumbnails =>
            Deviation.thumbs.OrEmpty().Select(t => new Thumb(t));
    };
}
