using Pandacap.UI.Elements;

namespace Pandacap.Database
{
    public class InboxFurAffinityJournal : InboxFurAffinityPost
    {
        public int JournalId { get; set; }

        public override string Url => $"https://www.furaffinity.net/journal/{JournalId}";

        public override IEnumerable<IPostThumbnail> Thumbnails => [];
    }
}
