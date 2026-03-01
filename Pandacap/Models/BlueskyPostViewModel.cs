using Pandacap.Clients.ATProto;

namespace Pandacap.Models
{
    public record BlueskyPostViewModel(
        string DID,
        string Handle,
        string? AvatarCID,
        ATProtoRecord<BlueskyPost> Record,
        bool IsInFavorites);
}
