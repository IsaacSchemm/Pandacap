using Pandacap.Database;

namespace Pandacap.Models
{
    public record TagReapplicationViewModel(
        IReadOnlyList<Post> Posts,
        IReadOnlyDictionary<Guid, string> ShortCodeLists,
        string? Next);
}
