namespace Pandacap.Data

open System
open System.ComponentModel.DataAnnotations
open Pandacap.Html
open Pandacap.PlatformBadges

type BlueskyFeedItemUser() =
    member val DID = "" with get, set
    member val PDS = nullString with get, set
    member val DisplayName = nullString with get, set
    member val Handle = "" with get, set
    member val Avatar = nullString with get, set

type BlueskyFeedItemImage() =
    member val Thumb = "" with get, set
    member val Fullsize = "" with get, set
    member val Alt = "" with get, set

type BlueskyFeedItem() =
    [<Key>]
    member val Id = Guid.Empty with get, set

    member val CID = "" with get, set
    member val RecordKey = "" with get, set
    member val Author = new BlueskyFeedItemUser() with get, set
    member val PostedBy = new BlueskyFeedItemUser() with get, set
    member val CreatedAt = DateTimeOffset.MinValue with get, set
    member val IndexedAt = DateTimeOffset.MinValue with get, set
    member val IsAdultContent = false with get, set
    member val Text = "" with get, set
    member val Images = new ResizeArray<BlueskyFeedItemImage>() with get, set
    member val DismissedAt = nullDateTimeOffset with get, set

    interface IBlueskyPost with
        member this.CID = this.CID
        member this.DID = this.Author.DID
        member this.PDS = this.Author.PDS
        member this.RecordKey = this.RecordKey
        member _.InFavorites = false

    interface IInboxPost with
        member this.DismissedAt
            with get () = this.DismissedAt
             and set value = this.DismissedAt <- value
        member _.IsPodcast = false
        member this.IsShare = this.PostedBy.DID <> this.Author.DID

    interface IPost with
        member _.Platform = Bluesky
        member this.Url = $"https://{this.PostedBy.PDS}"
        member this.DisplayTitle =
            TitleGenerator.FromBody this.Text
            |> ExcerptGenerator.FromText 60
        member this.Id = $"{this.Id}"
        member this.LinkUrl = $"/ATProto/ViewKnownBlueskyPost?id={this.Id}"
        member this.PostedAt = this.CreatedAt
        member this.ProfileUrl = $"https://bsky.app/profile/{this.PostedBy.DID}"
        member this.Thumbnails = [
            if not this.IsAdultContent then
                for image in this.Images do {
                    new IPostThumbnail with
                        member _.AltText = image.Alt
                        member _.Url = image.Thumb
                }
        ]
        member this.Usericon = this.PostedBy.Avatar
        member this.Username = this.PostedBy.Handle
