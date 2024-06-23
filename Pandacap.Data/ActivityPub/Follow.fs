namespace Pandacap.Data

open System
open System.ComponentModel.DataAnnotations

/// An ActivityPub actor who this Pandacap actor is following.
type Follow() =
    /// The follower's actor ID.
    [<Key>]
    member val ActorId = "" with get, set

    /// The date/time at which this follow was added.
    member val AddedAt = DateTimeOffset.MinValue with get, set

    /// The Pandacap-generated ID used for this activity when it was placed in the ActivityPubOutboundActivities table.
    member val FollowGuid = Guid.Empty with get, set

    /// Whether the follow has been accepted.
    member val Accepted = false with get, set

    /// This actor's personal ActivityPub inbox.
    member val Inbox = "" with get, set

    /// The shared inbox of this actor's ActivityPub server, if any.
    member val SharedInbox = nullString with get, set

    /// The preferred username of this actor, if any.
    member val PreferredUsername = nullString with get, set

    /// The icon URL of this actor, if any.
    member val IconUrl = nullString with get, set

    /// Whether to include image posts from other users shared (boosted) by this user.
    member val IncludeImageShares = false with get, set

    /// Whether to include text posts from other users shared (boosted) by this user.
    member val IncludeTextShares = false with get, set

    interface IRemoteActorRelationship with
        member this.ActorId = this.ActorId
        member this.IconUrl = this.IconUrl
        member this.Pending = not this.Accepted
        member this.PreferredUsername = this.PreferredUsername
