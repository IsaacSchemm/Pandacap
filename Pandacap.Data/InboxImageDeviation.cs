namespace Pandacap.Data
{
    public class InboxImageDeviation : InboxDeviation, IThumbnail, IImagePost
    {
        public class DeviantArtThumbnailRendition : IThumbnailRendition
        {
            public string Url { get; set; } = "";
            public int Width { get; set; }
            public int Height { get; set; }
        }

        public List<DeviantArtThumbnailRendition> ThumbnailRenditions { get; set; } = [];

        string? IThumbnail.AltText => null;

        IEnumerable<IThumbnail> IImagePost.Thumbnails => [this];

        IEnumerable<IThumbnailRendition> IThumbnail.Renditions => ThumbnailRenditions;
    }
}
