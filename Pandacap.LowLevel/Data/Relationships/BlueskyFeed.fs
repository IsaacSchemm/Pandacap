namespace Pandacap.Data

open System
open System.ComponentModel.DataAnnotations
open System.ComponentModel.DataAnnotations.Schema
open Pandacap.PlatformBadges

type BlueskyFeed() =
    [<Key>]
    member val DID = "" with get, set

    member val IncludeTextPosts = false with get, set
    member val IncludeImagePosts = false with get, set
    member val IncludeTextShares = false with get, set
    member val IncludeImageShares = false with get, set
    member val IncludeReplies = false with get, set
    member val IncludeQuotePosts = false with get, set

    member val Handle = nullString with get, set
    member val DisplayName = nullString with get, set
    member val Avatar = nullString with get, set

    member val LastRefreshedAt = DateTimeOffset.MinValue with get, set
    member val LastPostedAt = DateTimeOffset.MinValue with get, set

    [<NotMapped>]
    member this.ShouldRefresh =
        let sincePost = DateTimeOffset.UtcNow - this.LastPostedAt

        let timeToWait =
            if sincePost < TimeSpan.FromDays(3) then TimeSpan.FromHours(1)
            else if sincePost < TimeSpan.FromDays(28) then TimeSpan.FromDays(1)
            else TimeSpan.FromDays(7)

        let sinceRefresh = DateTimeOffset.UtcNow - this.LastRefreshedAt

        sinceRefresh > timeToWait

    interface IFollow with
        member _.Platform = Bluesky
        member this.IconUrl = this.Avatar
        member this.Username = this.DisplayName |> orString this.Handle
        member this.Url = $"https://bsky.app/profile/{Uri.EscapeDataString(this.DID)}"
