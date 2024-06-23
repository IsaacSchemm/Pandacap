namespace Pandacap.Data

open System
open System.ComponentModel.DataAnnotations

/// An ActivityPub actor who this Pandacap actor is following.
type Follow() =
    [<Key>]
    member val ActorId = "" with get, set

    member val AddedAt = DateTimeOffset.MinValue with get, set
    member val FollowGuid = Guid.Empty with get, set
    member val Accepted = false with get, set
    member val Inbox = "" with get, set
    member val SharedInbox = nullString with get, set
    member val PreferredUsername = nullString with get, set
    member val IconUrl = nullString with get, set
    member val IncludeImageShares = false with get, set
    member val IncludeTextShares = false with get, set

    interface IRemoteActorRelationship with
        member this.ActorId = this.ActorId
        member this.IconUrl = this.IconUrl
        member this.Pending = not this.Accepted
        member this.PreferredUsername = this.PreferredUsername
