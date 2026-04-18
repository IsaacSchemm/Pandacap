using Pandacap.ActivityPub.RemoteObjects.Models;
using Pandacap.ActivityPub.Replies.Interfaces;

namespace Pandacap.Models
{
    public record RemotePostViewModel
    {
        public required RemotePost RemotePost { get; init; }
        public required IReadOnlyList<IReply> Replies { get; init; }
        public required bool IsInFavorites { get; init; }
    }
}
