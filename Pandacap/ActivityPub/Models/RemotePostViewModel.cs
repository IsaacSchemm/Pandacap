using Pandacap.ActivityPub.RemoteObjects.Models;

namespace Pandacap.Models
{
    public record RemotePostViewModel
    {
        public required RemotePost RemotePost { get; init; }
        public required bool IsInFavorites { get; init; }
    }
}
