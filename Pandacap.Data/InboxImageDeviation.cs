namespace Pandacap.Data
{
    public class InboxImageDeviation : InboxDeviation, IThumbnail
    {
        public class DeviantArtThumbnailRendition : IThumbnailRendition
        {
            public string Url { get; set; } = "";
            public int Width { get; set; }
            public int Height { get; set; }
        }

        public List<DeviantArtThumbnailRendition> ThumbnailRenditions { get; set; } = [];

        public override IEnumerable<IThumbnail> Thumbnails => [this];

        string? IThumbnail.AltText => null;

        IEnumerable<IThumbnailRendition> IThumbnail.Renditions => ThumbnailRenditions;
    }
}
