namespace Pandacap.Data

open System
open Pandacap.ActivityPub
open Pandacap.PlatformBadges

/// An ActivityPub actor who this Pandacap actor is following.
type Follow() =
    inherit RemoteActorRelationship()

    member val FollowGuid = Guid.Empty with get, set
    member val Accepted = false with get, set
    member val IgnoreImages = false with get, set
    member val IncludeImageShares = false with get, set
    member val IncludeTextShares = false with get, set

    interface IActivityPubFollow with
        member this.ActorId = this.ActorId

    interface IFollow with
        member this.Filtered =
            not this.IncludeImageShares
            || not this.IncludeTextShares
        member _.Platform = ActivityPub
        member this.LinkUrl = this.Url |> orString this.ActorId
        member this.IconUrl = this.IconUrl
        member this.Username = this.PreferredUsername |> orString this.ActorId
        member this.Url = this.ActorId
