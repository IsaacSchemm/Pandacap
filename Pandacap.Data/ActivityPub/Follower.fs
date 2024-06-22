namespace Pandacap.Data

open System
open System.ComponentModel.DataAnnotations

/// An ActivityPub actor who is following this Pandacap actor.
type Follower() =

    /// The follower's actor ID.
    [<Key>]
    member val ActorId = "" with get, set

    /// The date/time at which this follower was added.
    member val AddedAt = DateTimeOffset.MinValue with get, set

    /// This actor's personal ActivityPub inbox.
    member val Inbox = "" with get, set

    /// The shared inbox of this actor's ActivityPub server, if any.
    member val SharedInbox = nullString with get, set

    /// The preferred username of this actor, if any.
    member val PreferredUsername = nullString with get, set

    /// The icon URL of this actor, if any.
    member val IconUrl = nullString with get, set

    interface IRemoteActorRelationship with
        member this.ActorId = this.ActorId
        member this.IconUrl = this.IconUrl
        member _.Pending = false
        member this.PreferredUsername = this.PreferredUsername
