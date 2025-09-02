using Microsoft.FSharp.Collections;
using Pandacap.Clients.ATProto;

namespace Pandacap.Models
{
    public record BlueskyPostInteractorViewModel(
        string DID,
        string AccountName,
        bool Liked);

    public record BlueskyPostViewModel(
        string DID,
        string Handle,
        string? AvatarCID,
        ATProtoRecord<BlueskyPost> Record,
        bool IsInFavorites,
        FSharpList<BlueskyPostInteractorViewModel> MyProfiles,
        string? BridgyFedObjectId);
}
