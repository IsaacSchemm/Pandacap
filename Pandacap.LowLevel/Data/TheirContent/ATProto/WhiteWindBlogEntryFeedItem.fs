namespace Pandacap.Data

open System
open System.ComponentModel.DataAnnotations
open Pandacap.Html
open Pandacap.PlatformBadges

type WhiteWindBlogEntryFeedItemAuthor() =
    member val DID = "" with get, set
    member val PDS = "" with get, set
    member val DisplayName = nullString with get, set
    member val Handle = "" with get, set
    member val AvatarCID = nullString with get, set

type WhiteWindBlogEntryFeedItem() =
    [<Key>]
    member val CID = "" with get, set

    member val RecordKey = "" with get, set
    member val Author = new WhiteWindBlogEntryFeedItemAuthor() with get, set
    member val CreatedAt = DateTimeOffset.MinValue with get, set
    member val Title = nullString with get, set
    member val Content = "" with get, set
    member val DismissedAt = nullDateTimeOffset with get, set

    interface IInboxPost with
        member this.DismissedAt
            with get () = this.DismissedAt
             and set value = this.DismissedAt <- value
        member _.IsPodcast = false
        member _.IsShare = false

    interface IPost with
        member _.Platform = WhiteWind
        member this.Url = $"https://{this.Author.PDS}"
        member this.DisplayTitle =
            this.Title
            |> Option.ofObj
            |> Option.defaultWith (fun () ->
                TitleGenerator.FromBody this.Content
                |> ExcerptGenerator.FromText 60)
        member this.Id = $"{this.CID}"
        member this.InternalUrl = $"https://whtwnd.com/{this.Author.DID}/{this.RecordKey}"
        member this.ExternalUrl = $"https://whtwnd.com/{this.Author.DID}/{this.RecordKey}"
        member this.PostedAt = this.CreatedAt
        member this.ProfileUrl = $"https://whtwnd.com/{this.Author.DID}"
        member _.Thumbnails = []
        member this.Usericon =
            if not (isNull this.Author.AvatarCID)
            then $"/ATProto/GetBlob?did={this.Author.DID}&cid={this.Author.AvatarCID}"
            else null
        member this.Username = this.Author.Handle
