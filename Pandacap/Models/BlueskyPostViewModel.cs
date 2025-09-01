using Microsoft.FSharp.Collections;
using Pandacap.Clients;

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
        ATProtoClient.Repo.RecordListItem<ATProtoClient.Repo.Schemas.Bluesky.Feed.Post> Record,
        bool IsInFavorites,
        FSharpList<BlueskyPostInteractorViewModel> MyProfiles,
        string? BridgyFedObjectId);
}
