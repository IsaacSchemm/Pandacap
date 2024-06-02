namespace Pandacap.Data
{
    public class DeviantArtInboxThumbnail : IThumbnailRendition
    {
        public string Url { get; set; } = "";
        public int Width { get; set; }
        public int Height { get; set; }
    }

    public class DeviantArtInboxArtworkPost : DeviantArtInboxPost, IThumbnail, IImagePost
    {
        public List<DeviantArtInboxThumbnail> ThumbnailRenditions { get; set; } = [];

        string? IThumbnail.AltText => null;

        IEnumerable<IThumbnail> IImagePost.Thumbnails => [this];

        IEnumerable<IThumbnailRendition> IThumbnail.Renditions => ThumbnailRenditions;
    }
}
