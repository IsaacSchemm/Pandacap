using Microsoft.FSharp.Collections;
using Pandacap.Clients.ATProto.Public;

namespace Pandacap.Models
{
    public record BlueskyPostInteractorViewModel(
        string DID,
        string AccountName,
        bool Liked);

    public record BlueskyPostViewModel(
        Guid Id,
        Profile.ProfileResponse ProfileResponse,
        BlueskyFeed.Post Post,
        bool IsInFavorites,
        FSharpList<BlueskyPostInteractorViewModel> MyProfiles,
        string? BridgyFedObjectId,
        string? BridgyFedHandle);
}
