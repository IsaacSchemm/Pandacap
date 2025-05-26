namespace Pandacap.Data

open System
open System.ComponentModel.DataAnnotations.Schema

/// An ActivityPub actor that represents a Lemmy community.
type CommunityBookmark() =
    inherit RemoteActorRelationship()

    [<NotMapped>]
    member this.Uri =
        new Uri(this.ActorId)

    [<NotMapped>]
    member this.Host =
        this.Uri.Host

    [<NotMapped>]
    member this.Name =
        this.PreferredUsername
