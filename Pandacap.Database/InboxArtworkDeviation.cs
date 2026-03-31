using Pandacap.UI.Elements;

namespace Pandacap.Database
{
    public class InboxArtworkDeviation : InboxDeviation, IPostThumbnail
    {
        public string ThumbnailUrl { get; set; } = "";

        public override IEnumerable<IPostThumbnail> Thumbnails => [this];

        string IPostThumbnail.Url => ThumbnailUrl;

        string IPostThumbnail.AltText => "";
    }
}
