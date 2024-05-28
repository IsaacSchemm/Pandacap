namespace Pandacap.Data
{
    public class ActivityPubImageAttachment
    {
        public string Url { get; set; } = "";
        public string? Name { get; set; }
    }

    public class ActivityPubInboxImagePost : ActivityPubInboxPost
    {
        public List<ActivityPubImageAttachment> Attachments { get; set; } = [];

        public string? ThumbnailUrl => Attachments.Select(x => x.Url).FirstOrDefault();
    }
}
