using Pandacap.Database;

namespace Pandacap.Models
{
    public class FollowerViewModel
    {
        public required IEnumerable<Follower> Items { get; set; }
    }
}
