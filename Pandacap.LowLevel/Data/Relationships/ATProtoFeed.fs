namespace Pandacap.Data

open System.ComponentModel.DataAnnotations
open System.ComponentModel.DataAnnotations.Schema
open Pandacap.PlatformBadges

type ATProtoFeedFilters() =
    member val IgnoreImages = false with get, set
    member val SkipPostsWithoutImages = false with get, set
    member val SkipReplies = false with get, set
    member val SkipQuotePosts = false with get, set

    [<NotMapped>]
    member this.SkipAny =
        this.SkipPostsWithoutImages
        || this.SkipReplies
        || this.SkipQuotePosts

type ATProtoFeedCollection() =
    member val NSID = "" with get, set
    member val LastSeenCIDs = new ResizeArray<string>() with get, set
    member val Filters = new ATProtoFeedFilters() with get, set

type ATProtoFeed() =
    [<Key>]
    member val DID = "" with get, set

    member val PDS = "" with get, set

    member val Collections = new ResizeArray<ATProtoFeedCollection>() with get, set

    member val Handle = nullString with get, set
    member val DisplayName = nullString with get, set
    member val Avatar = nullString with get, set

    interface IFollow with
        member this.Filtered =
            this.Collections
            |> Seq.exists (fun c -> c.Filters.SkipAny)
        member _.Platform = ATProto
        member this.IconUrl = this.Avatar
        member this.LinkUrl = $"https://bsky.app/profile/{this.Handle}"
        member this.Username = this.Handle
        member this.Url = $"https://{this.PDS}/xrpc/app.bsky.actor.getProfile?actor={this.DID}"
