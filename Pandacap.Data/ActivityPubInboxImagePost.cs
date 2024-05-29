namespace Pandacap.Data
{
    public class ActivityPubImageAttachment : IThumbnail
    {
        public string Url { get; set; } = "";
        public string? Name { get; set; }

        string? IThumbnail.ThumbnailUrl => Url;
        string? IThumbnail.ThumbnailSrcset => null;
        string? IThumbnail.AltText => Name;
    }

    public class ActivityPubInboxImagePost : ActivityPubInboxPost, IImagePost
    {
        public List<ActivityPubImageAttachment> Attachments { get; set; } = [];

        public string? ThumbnailUrl => Attachments.Select(x => x.Url).FirstOrDefault();

        IEnumerable<IThumbnail> IImagePost.Thumbnails => Attachments;
    }
}
