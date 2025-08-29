namespace Pandacap.Data

open System.ComponentModel.DataAnnotations
open Pandacap.PlatformBadges

type ATProtoFeedCollection() =
    member val NSID = "" with get, set
    member val LastSeenCIDs = new ResizeArray<string>() with get, set

type ATProtoFeed() =
    [<Key>]
    member val DID = "" with get, set

    member val PDS = "" with get, set

    member val Collections = new ResizeArray<ATProtoFeedCollection>() with get, set

    member val IncludePostsWithoutImages = true with get, set
    member val IncludeReplies = false with get, set
    member val IncludeQuotePosts = true with get, set

    member val IgnoreImages = false with get, set

    member val Handle = nullString with get, set
    member val DisplayName = nullString with get, set
    member val AvatarCID = nullString with get, set

    interface IFollow with
        member this.Filtered =
            not this.IncludePostsWithoutImages || not this.IncludeQuotePosts
        member _.Platform = ATProto
        member this.IconUrl =
            if isNull this.AvatarCID
            then null
            else $"https://{this.PDS}/xrpc/com.atproto.sync.getBlob?did={this.DID}&cid={this.AvatarCID}"
        member this.LinkUrl = $"https://bsky.app/profile/{this.Handle}"
        member this.Username = this.Handle
        member this.Url = $"https://{this.PDS}/xrpc/app.bsky.actor.getProfile?actor={this.DID}"
