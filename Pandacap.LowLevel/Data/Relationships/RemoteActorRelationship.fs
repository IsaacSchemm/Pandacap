namespace Pandacap.Data

open System
open System.ComponentModel.DataAnnotations

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
    member val Url = nullString with get, set
