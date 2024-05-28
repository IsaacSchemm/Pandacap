using Pandacap.Data;

namespace Pandacap.Models.Inbox
{
    public class ListViewModel
    {
        public string Action { get; set; } = "";

        public IEnumerable<IInboxPost> InboxItems { get; set; } = [];

        public int? PrevOffset { get; set; }

        public int? NextOffset { get; set; }

        public int Count { get; set; }
    }
}
