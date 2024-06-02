namespace Pandacap.Data
{
    public class DeviantArtOurImage
    {
        public string Url { get; set; } = "";
        public string ContentType { get; set; } = "";
        public int Width { get; set; }
        public int Height { get; set; }
    }

    public class DeviantArtOurThumbnail : IThumbnailRendition
    {
        public string Url { get; set; } = "";
        public int Width { get; set; }
        public int Height { get; set; }
    }

    public class DeviantArtArtworkDeviation : DeviantArtDeviation, IThumbnail, IImagePost
    {
        public DeviantArtOurImage Image { get; set; } = new DeviantArtOurImage();

        public List<DeviantArtOurThumbnail> ThumbnailRenditions { get; set; } = [];

        public string? AltText { get; set; }

        public override bool RenderAsArticle => false;

        IEnumerable<IThumbnail> IImagePost.Thumbnails => [this];

        IEnumerable<IThumbnailRendition> IThumbnail.Renditions => ThumbnailRenditions;
    }
}
