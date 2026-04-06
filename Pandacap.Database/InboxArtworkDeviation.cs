using Pandacap.UI.Elements;

namespace Pandacap.Database
{
    public class InboxArtworkDeviation : InboxDeviation
    {
        public string? ThumbnailUrl { get; set; }

        public override IEnumerable<IPostThumbnail> Thumbnails => ThumbnailUrl is string url
            ? [new PostThumbnail(url)]
            : [];

        private record PostThumbnail(string Url) : IPostThumbnail
        {
            string IPostThumbnail.AltText => "";
        }
    }
}
