using Pandacap.UI.Elements;

namespace Pandacap.Models
{
    public class FollowingAndFeedsViewModel
    {
        public required IEnumerable<IFollow> Items { get; set; }
        public required IEnumerable<string> ProblemInboxes { get; set; }
    }
}
