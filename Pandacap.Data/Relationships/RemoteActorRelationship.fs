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
