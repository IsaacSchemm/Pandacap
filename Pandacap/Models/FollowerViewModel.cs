using Pandacap.Database;

namespace Pandacap.Models
{
    public class FollowerViewModel
    {
        public required IEnumerable<ActivityPubFollower> Items { get; set; }
    }
}
