namespace Pandacap.Data

open System
open System.ComponentModel.DataAnnotations

/// An ActivityPub actor who is following this Pandacap actor.
type Follower() =
    [<Key>]
    member val ActorId = "" with get, set

    member val AddedAt = DateTimeOffset.MinValue with get, set
    member val Inbox = "" with get, set
    member val SharedInbox = nullString with get, set
    member val PreferredUsername = nullString with get, set
    member val IconUrl = nullString with get, set

    interface IRemoteActorRelationship with
        member this.ActorId = this.ActorId
        member this.IconUrl = this.IconUrl
        member _.Pending = false
        member this.PreferredUsername = this.PreferredUsername
