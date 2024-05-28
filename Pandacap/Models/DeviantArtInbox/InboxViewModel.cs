using Pandacap.Data;

namespace Pandacap.Models.DeviantArtInbox
{
    public class InboxViewModel
    {
        public string Action { get; set; } = "";

        public IEnumerable<DeviantArtInboxPost> InboxItems { get; set; } = [];

        public int? PrevOffset { get; set; }

        public int? NextOffset { get; set; }

        public int Count { get; set; }
    }
}
