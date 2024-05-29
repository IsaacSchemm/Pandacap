namespace Pandacap.Data
{
    public class ActivityPubImageAttachment : IImage
    {
        public string Url { get; set; } = "";
        public string? Name { get; set; }

        string? IImage.ThumbnailUrl => Url;
        string? IImage.ThumbnailSrcset => null;
        string? IImage.AltText => Name;
    }

    public class ActivityPubInboxImagePost : ActivityPubInboxPost, IImagePost
    {
        public List<ActivityPubImageAttachment> Attachments { get; set; } = [];

        public string? ThumbnailUrl => Attachments.Select(x => x.Url).FirstOrDefault();

        IEnumerable<IImage> IImagePost.Images => Attachments;
    }
}
