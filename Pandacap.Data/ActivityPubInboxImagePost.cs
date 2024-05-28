namespace Pandacap.Data
{
    public class ActivityPubImageAttachment : IInboxImage
    {
        public string Url { get; set; } = "";
        public string? Name { get; set; }

        string? IInboxImage.ThumbnailUrl => Url;
        string? IInboxImage.ThumbnailSrcset => null;
        string? IInboxImage.AltText => Name;
    }

    public class ActivityPubInboxImagePost : ActivityPubInboxPost, IInboxImagePost
    {
        public List<ActivityPubImageAttachment> Attachments { get; set; } = [];

        public string? ThumbnailUrl => Attachments.Select(x => x.Url).FirstOrDefault();

        IEnumerable<IInboxImage> IInboxImagePost.Images => Attachments;
    }
}
