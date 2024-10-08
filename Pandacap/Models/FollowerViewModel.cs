using Pandacap.Data;

namespace Pandacap.Models
{
    public class FollowerViewModel : ListViewModel<Follower>
    {
        public required IEnumerable<string> GhostedActors { get; init; }
    }
}
