namespace Pandacap.Models.Inbox
{
    public class InboxViewModel<T>
    {
        public string Action { get; set; } = "";

        public IEnumerable<T> InboxItems { get; set; } = [];

        public int? PrevOffset { get; set; }

        public int? NextOffset { get; set; }

        public int Count { get; set; }
    }
}
