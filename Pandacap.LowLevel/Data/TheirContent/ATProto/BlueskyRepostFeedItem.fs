namespace Pandacap.Data

open System
open System.ComponentModel.DataAnnotations
open Pandacap.Html
open Pandacap.PlatformBadges
open System.ComponentModel.DataAnnotations.Schema

type BlueskyRepostFeedItemOriginal() =
    member val CID = "" with get, set
    member val DID = "" with get, set
    member val PDS = "" with get, set
    member val RecordKey = "" with get, set

type BlueskyRepostFeedItemUser() =
    member val DID = "" with get, set
    member val PDS = "" with get, set
    member val DisplayName = nullString with get, set
    member val Handle = "" with get, set
    member val AvatarCID = nullString with get, set

type BlueskyRepostFeedItemImage() =
    member val CID = "" with get, set
    member val Alt = "" with get, set

type BlueskyRepostFeedItem() =
    [<Key>]
    member val CID = "" with get, set
    
    member val Original = new BlueskyRepostFeedItemOriginal() with get, set
    member val RepostedBy = new BlueskyRepostFeedItemUser() with get, set
    member val CreatedAt = DateTimeOffset.MinValue with get, set
    member val RepostedAt = DateTimeOffset.MinValue with get, set
    member val Labels = new ResizeArray<string>() with get, set
    member val Text = "" with get, set
    member val Images = new ResizeArray<BlueskyRepostFeedItemImage>() with get, set
    member val DismissedAt = nullDateTimeOffset with get, set

    [<NotMapped>]
    member this.AdultContent = Seq.head (seq {
        for l in this.Labels do
            match l with
            | "porn"
            | "sexual"
            | "nudity"
            | "sexual-figurative"
            | "graphic-media" -> true
            | _ -> ()

        false
    })

    interface IBlueskyPost with
        member this.CID = this.Original.CID
        member this.DID = this.Original.DID
        member this.PDS = this.Original.PDS
        member this.RecordKey = this.Original.RecordKey
        member _.InFavorites = false

    interface IInboxPost with
        member this.DismissedAt
            with get () = this.DismissedAt
             and set value = this.DismissedAt <- value
        member _.IsPodcast = false
        member _.IsShare = true

    interface IPost with
        member _.Platform = Bluesky
        member this.Url = $"https://{this.Original.PDS}"
        member this.DisplayTitle =
            TitleGenerator.FromBody this.Text
            |> ExcerptGenerator.FromText 60
        member this.Id = $"{this.CID}"
        member this.InternalUrl = $"/ATProto/ViewBlueskyPost?did={this.Original.DID}&rkey={this.Original.RecordKey}"
        member this.ExternalUrl = $"https://bsky.app/profile/{this.Original.DID}/post/{this.Original.RecordKey}"
        member this.PostedAt = this.RepostedAt
        member this.ProfileUrl = $"https://bsky.app/profile/{this.RepostedBy.DID}"
        member this.Thumbnails = [
            if not this.AdultContent then
                for image in this.Images do {
                    new IPostThumbnail with
                        member _.AltText = image.Alt
                        member _.Url = $"/ATProto/GetBlob?did={this.Original.DID}&cid={image.CID}"
                }
        ]
        member this.Usericon =
            if not (isNull this.RepostedBy.AvatarCID)
            then $"/ATProto/GetBlob?did={this.RepostedBy.DID}&cid={this.RepostedBy.AvatarCID}"
            else null
        member this.Username = this.RepostedBy.Handle
