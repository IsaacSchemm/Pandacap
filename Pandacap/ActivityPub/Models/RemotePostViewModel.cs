using Pandacap.ActivityPub.Models.Inbound;

namespace Pandacap.Models
{
    public record RemotePostViewModel
    {
        public required RemotePost RemotePost { get; init; }
        public required bool IsInFavorites { get; init; }
    }
}
