using DeviantArtFs.Extensions;
using DeviantArtFs.ResponseTypes;

namespace Pandacap.Models
{
    public record AltTextPageItem(
        Deviation Deviation,
        string? AltText)
    {
        public string? ThumbnailUrl => Deviation.thumbs.OrEmpty()
            .OrderBy(x => x.height >= 200 ? 1 : 2)
            .ThenBy(x => x.height)
            .Select(x => x.src)
            .FirstOrDefault();
    };
}
