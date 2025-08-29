namespace Pandacap.Data

open System
open System.ComponentModel.DataAnnotations
open Pandacap.Html
open Pandacap.PlatformBadges
open System.ComponentModel.DataAnnotations.Schema

type BlueskyPostFeedItemUser() =
    member val DID = "" with get, set
    member val PDS = nullString with get, set
    member val DisplayName = nullString with get, set
    member val Handle = "" with get, set
    member val AvatarCID = nullString with get, set

type BlueskyPostFeedItemImage() =
    member val CID = "" with get, set
    member val Alt = "" with get, set

type BlueskyPostFeedItem() =
    [<Key>]
    member val CID = "" with get, set

    member val RecordKey = "" with get, set
    member val Author = new BlueskyPostFeedItemUser() with get, set
    member val CreatedAt = DateTimeOffset.MinValue with get, set
    member val Labels = new ResizeArray<string>() with get, set
    member val Text = "" with get, set
    member val Images = new ResizeArray<BlueskyPostFeedItemImage>() with get, set
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
        member _.IsShare = false

    interface IPost with
        member _.Platform = Bluesky
        member this.Url = $"https://{this.Author.PDS}"
        member this.DisplayTitle =
            TitleGenerator.FromBody this.Text
            |> ExcerptGenerator.FromText 60
        member this.Id = $"{this.CID}"
        member this.InternalUrl = $"/ATProto/ViewBlueskyPost?did={this.Author.DID}&rkey={this.RecordKey}"
        member this.ExternalUrl = $"https://bsky.app/profile/{this.Author.DID}/post/{this.RecordKey}"
        member this.PostedAt = this.CreatedAt
        member this.ProfileUrl = $"https://bsky.app/profile/{this.Author.DID}"
        member this.Thumbnails = [
            if not this.AdultContent then
                for image in this.Images do {
                    new IPostThumbnail with
                        member _.AltText = image.Alt
                        member _.Url = $"https://{this.Author.PDS}/xrpc/com.atproto.sync.getBlob?did={this.Author.DID}&cid={image.CID}"
                }
        ]
        member this.Usericon = $"https://{this.Author.PDS}/xrpc/com.atproto.sync.getBlob?did={this.Author.DID}&cid={this.Author.AvatarCID}"
        member this.Username = this.Author.Handle
