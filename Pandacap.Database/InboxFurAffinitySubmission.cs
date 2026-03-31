using Pandacap.UI.Elements;

namespace Pandacap.Database
{
    public class InboxFurAffinitySubmission : InboxFurAffinityPost, IPostThumbnail
    {
        public int SubmissionId { get; set; }

        public string Thumbnail { get; set; } = "";

        public string Link { get; set; } = "";

        public bool Sfw { get; set; }

        public override string Url => Link;

        public override IEnumerable<IPostThumbnail> Thumbnails => [this];

        string IPostThumbnail.Url => Sfw ? Thumbnail : "/images/trgray.svg";

        string IPostThumbnail.AltText => "";
    }
}
