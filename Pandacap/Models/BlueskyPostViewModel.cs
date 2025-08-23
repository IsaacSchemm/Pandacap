using Microsoft.FSharp.Collections;
using Pandacap.Clients;

namespace Pandacap.Models
{
    public record BlueskyPostInteractorViewModel(
        string DID,
        string AccountName,
        bool Liked);

    public record BlueskyPostViewModel(
        ATProtoClient.Bluesky.Feed.PostThread Thread,
        bool IsInFavorites,
        FSharpList<BlueskyPostInteractorViewModel> MyProfiles,
        string? BridgyFedObjectId,
        string? BridgyFedHandle);
}
