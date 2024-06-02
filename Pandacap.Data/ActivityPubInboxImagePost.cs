namespace Pandacap.Data
{
    public class ActivityPubImageAttachment : IThumbnail, IThumbnailRendition
    {
        public string Url { get; set; } = "";
        public string? Name { get; set; }

        int IThumbnailRendition.Width => 0;
        int IThumbnailRendition.Height => 0;

        string? IThumbnail.AltText => Name;
        IEnumerable<IThumbnailRendition> IThumbnail.Renditions => [this];
    }

    public class ActivityPubInboxImagePost : ActivityPubInboxPost, IImagePost
    {
        public List<ActivityPubImageAttachment> Attachments { get; set; } = [];

        public string? ThumbnailUrl => Attachments.Select(x => x.Url).FirstOrDefault();

        IEnumerable<IThumbnail> IImagePost.Thumbnails => Attachments;
    }
}
