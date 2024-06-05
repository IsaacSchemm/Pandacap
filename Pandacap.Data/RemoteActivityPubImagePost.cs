namespace Pandacap.Data
{
    public class RemoteActivityPubImagePost : RemoteActivityPubPost
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

        public List<ActivityPubImageAttachment> Attachments { get; set; } = [];

        public override IEnumerable<IThumbnail> Thumbnails => Attachments;
    }
}
