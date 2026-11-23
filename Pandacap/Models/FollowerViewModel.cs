using Pandacap.Database;

namespace Pandacap.Models
{
    public class FollowerViewModel
    {
        public required IEnumerable<ActivityPubFollower> Items { get; set; }
        public required IEnumerable<string> ProblemInboxes { get; set; }
    }
}
