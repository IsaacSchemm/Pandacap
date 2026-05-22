using DeviantArtFs.ResponseTypes;
using Microsoft.FSharp.Core;

namespace Pandacap.Models
{
    public record AltTextPageItem(
        Deviation Deviation,
        string? AltText)
    {
        public string? ThumbnailUrl =>
            OptionModule.DefaultValue(
                [],
                Deviation.thumbs)
            .OrderBy(x => x.height >= 200 ? 1 : 2)
            .ThenBy(x => x.height)
            .Select(x => x.src)
            .FirstOrDefault();
    };
}
