namespace Pandacap.Data

open System.Collections.Generic
open System.ComponentModel.DataAnnotations
open Pandacap.PlatformBadges

type ATProtoFeed() =
    [<Key>]
    member val DID = "" with get, set

    member val CurrentPDS = "" with get, set

    member val IncludePostsWithoutImages = true with get, set
    member val IncludeReplies = false with get, set
    member val IncludeQuotePosts = true with get, set

    member val IgnoreImages = false with get, set

    member val Handle = nullString with get, set
    member val DisplayName = nullString with get, set
    member val AvatarCID = nullString with get, set

    member val NSIDs = new ResizeArray<string>() with get, set
    member val Cursors = new Dictionary<string, string>() with get, set

    member val LastCommitCID = nullString with get, set

    interface IFollow with
        member this.Filtered =
            not this.IncludePostsWithoutImages || not this.IncludeQuotePosts
        member _.Platform = ATProto
        member this.IconUrl =
            if isNull this.AvatarCID
            then null
            else $"/ATProto/GetBlob?did={this.DID}&cid={this.AvatarCID}"
        member this.LinkUrl = $"https://bsky.app/profile/{this.Handle}"
        member this.Username = this.DisplayName |> orString this.Handle
        member this.Url = $"https://{this.CurrentPDS}/xrpc/app.bsky.actor.getProfile?actor={this.DID}"
