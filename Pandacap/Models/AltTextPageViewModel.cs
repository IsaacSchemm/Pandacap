namespace Pandacap.Models
{
    public class AltTextPageViewModel
    {
        public IReadOnlyList<AltTextPageItem> Items { get; set; } = [];

        public Guid? Next { get; set; }
    }
}
