using Pandacap.Data;

namespace Pandacap.Models
{
    public class FollowerViewModel
    {
        public required IEnumerable<Follower> Items { get; set; }
        public required IEnumerable<string> GhostedActors { get; init; }
    }
}
