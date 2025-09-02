using Microsoft.FSharp.Collections;
using Pandacap.Clients;
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
        Lexicon.IRecord<Lexicon.App.Bsky.Feed.Post> Record,
        bool IsInFavorites,
        FSharpList<BlueskyPostInteractorViewModel> MyProfiles,
        string? BridgyFedObjectId);
}
