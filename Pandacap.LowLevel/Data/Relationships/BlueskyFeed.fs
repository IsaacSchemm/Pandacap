namespace Pandacap.Data

open System
open System.ComponentModel.DataAnnotations
open System.ComponentModel.DataAnnotations.Schema
open Pandacap.PlatformBadges

type BlueskyFeed() =
    [<Key>]
    member val DID = "" with get, set

    member val PDS = "public.api.bsky.app" with get, set

    member val IgnoreImages = false with get, set

    member val IncludeTextShares = true with get, set
    member val IncludeImageShares = true with get, set
    member val IncludeQuotePosts = true with get, set

    member val Handle = nullString with get, set
    member val DisplayName = nullString with get, set
    member val Avatar = nullString with get, set

    member val LastRefreshedAt = DateTimeOffset.MinValue with get, set
    member val LastPostedAt = DateTimeOffset.MinValue with get, set

    [<NotMapped>]
    member this.ShouldRefresh =
        let sincePost = DateTimeOffset.UtcNow - this.LastPostedAt

        let timeToWait =
            if sincePost < TimeSpan.FromDays(7)
            then TimeSpan.Zero
            else TimeSpan.FromDays(1)

        let sinceRefresh = DateTimeOffset.UtcNow - this.LastRefreshedAt

        sinceRefresh > timeToWait

    interface IFollow with
        member this.Filtered =
            not this.IncludeTextShares
            || not this.IncludeImageShares
            || not this.IncludeQuotePosts
        member _.Platform = Bluesky
        member this.IconUrl = this.Avatar
        member this.LinkUrl = $"https://bsky.app/profile/{this.Handle}"
        member this.Username = this.Handle
        member this.Url = $"https://{this.PDS}/xrpc/app.bsky.actor.getProfile?actor={this.DID}"
