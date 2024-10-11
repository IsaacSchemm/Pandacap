using Pandacap.LowLevel;

namespace Pandacap.Models
{
    public record CommunityViewModel(
        Lemmy.Community Community,
        IEnumerable<Lemmy.PostObject> PostObjects);
}
