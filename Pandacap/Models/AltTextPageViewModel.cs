namespace Pandacap.Models
{
    public class AltTextPageViewModel
    {
        public int? PrevOffset { get; set; }

        public int? NextOffset { get; set; }

        public IReadOnlyList<AltTextPageItem> Items { get; set; } = [];
    }
}
