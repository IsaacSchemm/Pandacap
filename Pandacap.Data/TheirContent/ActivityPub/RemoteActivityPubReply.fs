namespace Pandacap.Data

open System
open System.ComponentModel.DataAnnotations

/// A remote ActivityPub post that is a reply to one of this app's posts (a UserPost or AddressedPost).
type RemoteActivityPubReply() =
    [<Key>]
    member val Id = Guid.Empty with get, set

    member val ObjectId = "" with get, set
    member val InReplyTo = Guid.Empty with get, set

    member val Public = false with get, set
    member val Approved = false with get, set

    member val CreatedBy = "" with get, set
    member val Username = nullString with get, set
    member val Usericon = nullString with get, set
    member val CreatedAt = DateTimeOffset.MinValue with get, set
    member val Summary = nullString with get, set
    member val Sensitive = false with get, set
    member val Name = nullString with get, set
    member val Content = nullString with get, set
