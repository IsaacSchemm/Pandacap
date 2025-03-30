namespace Pandacap.Data

open System
open System.ComponentModel.DataAnnotations
open Pandacap.Html
open Pandacap.PlatformBadges

type InboxBlueskyUser() =
    member val DID = "" with get, set
    member val PDS = nullString with get, set
    member val DisplayName = nullString with get, set
    member val Handle = "" with get, set
    member val Avatar = nullString with get, set

type InboxBlueskyImage() =
    member val Thumb = "" with get, set
    member val Fullsize = "" with get, set
    member val Alt = "" with get, set

type InboxBlueskyPost() =
    [<Key>]
    member val Id = Guid.Empty with get, set

    member val CID = "" with get, set
    member val RecordKey = "" with get, set
    member val Author = new InboxBlueskyUser() with get, set
    member val PostedBy = new InboxBlueskyUser() with get, set
    member val CreatedAt = DateTimeOffset.MinValue with get, set
    member val IndexedAt = DateTimeOffset.MinValue with get, set
    member val IsAdultContent = false with get, set
    member val Text = "" with get, set
    member val Images = new ResizeArray<InboxBlueskyImage>() with get, set
    member val DismissedAt = nullDateTimeOffset with get, set

    interface IInboxPost with
        member this.DismissedAt
            with get () = this.DismissedAt
             and set value = this.DismissedAt <- value
        member _.IsPodcast = false
        member this.IsShare = this.PostedBy.DID <> this.Author.DID

    interface IPost with
        member this.Badges = [
            match Option.ofObj this.PostedBy.PDS with
            | Some pds -> PostPlatform.GetBadge Bluesky |> Badge.WithParenthetical pds
            | None -> PostPlatform.GetBadge Bluesky
        ]
        member this.DisplayTitle = ExcerptGenerator.FromText 60 this.Text
        member this.Id = $"{this.Id}"
        member this.LinkUrl = $"https://bsky.app/profile/{this.Author.DID}/post/{this.RecordKey}"
        member this.ProfileUrl = $"https://bsky.app/profile/{this.PostedBy.DID}"
        member this.Thumbnails = [
            if not this.IsAdultContent then
                for image in this.Images do {
                    new IPostThumbnail with
                        member _.AltText = image.Alt
                        member _.Url = image.Thumb
                }
        ]
        member this.Timestamp = this.IndexedAt
        member this.Usericon = this.PostedBy.Avatar
        member this.Username = this.PostedBy.Handle
