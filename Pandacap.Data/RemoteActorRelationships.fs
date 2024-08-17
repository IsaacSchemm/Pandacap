namespace Pandacap.Data

open System
open System.ComponentModel.DataAnnotations
open System.ComponentModel.DataAnnotations.Schema

/// A relationship of some sort between Pandacap and a remote ActivityPub actor. Used in the frontend for paged lists of follows and followers.
type IRemoteActorRelationship =
    abstract member ActorId: string
    abstract member PreferredUsername: string
    abstract member IconUrl: string
    abstract member Pending: bool

/// A relationship of some sort between Pandacap and a remote ActivityPub actor.
[<AbstractClass>]
type RemoteActorRelationship() =
    [<Key>]
    member val ActorId = "" with get, set

    member val AddedAt = DateTimeOffset.MinValue with get, set
    member val Inbox = "" with get, set
    member val SharedInbox = nullString with get, set
    member val PreferredUsername = nullString with get, set
    member val IconUrl = nullString with get, set

    [<NotMapped>]
    abstract member Pending: bool

    interface IRemoteActorRelationship with
        member this.ActorId = this.ActorId
        member this.IconUrl = this.IconUrl
        member this.Pending = this.Pending
        member this.PreferredUsername = this.PreferredUsername

/// An ActivityPub actor who this Pandacap actor is following.
type Follow() =
    inherit RemoteActorRelationship()

    member val FollowGuid = Guid.Empty with get, set
    member val Accepted = false with get, set
    member val IncludeImageShares = false with get, set
    member val IncludeTextShares = false with get, set

    override this.Pending = not this.Accepted

/// An ActivityPub actor who is following this Pandacap actor.
type Follower() =
    inherit RemoteActorRelationship()

    member val GhostedSince = nullDateTimeOffset with get, set

    override _.Pending = false
