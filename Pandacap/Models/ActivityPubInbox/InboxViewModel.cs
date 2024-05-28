using Pandacap.Data;

namespace Pandacap.Models.ActivityPubInbox
{
    public class InboxViewModel
    {
        public string Action { get; set; } = "";

        public IEnumerable<ActivityPubInboxPost> InboxItems { get; set; } = [];

        public int? PrevOffset { get; set; }

        public int? NextOffset { get; set; }

        public int Count { get; set; }
    }
}
