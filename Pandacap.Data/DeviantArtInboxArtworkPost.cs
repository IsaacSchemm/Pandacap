namespace Pandacap.Data
{
    public class DeviantArtInboxThumbnail
    {
        public string Url { get; set; } = "";
        public int Width { get; set; }
        public int Height { get; set; }
    }

    public class DeviantArtInboxArtworkPost : DeviantArtInboxPost
    {
        public List<DeviantArtInboxThumbnail> Thumbnails { get; set; } = [];
    }
}
