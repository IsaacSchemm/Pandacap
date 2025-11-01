namespace Pandacap.Data

open System
open System.ComponentModel.DataAnnotations
open Pandacap.PlatformBadges

[<AbstractClass>]
type ATProtoFeedItem() =
    [<Key>]
    member val CID = "" with get, set

    member val PDS = "" with get, set
    member val DID = "" with get, set
    member val Collection = "" with get, set
    member val RecordKey = "" with get, set

    member val FeedTitle = "" with get, set
    member val FeedIconUrl = nullString with get, set

    member val Title = "" with get, set

    member val Timestamp = DateTimeOffset.MinValue with get, set

    member val ThumbnailUrl = nullString with get, set
    member val ThumbnailAltText = nullString with get, set

    interface IPost with
        member _.Platform = ATProto
        member this.Url = $"https://{this.PDS}"
        member this.DisplayTitle = this.Title
        member this.Id = $"{this.CID}"
        member this.InternalUrl = $"/ATProto/RedirectTo?pds={this.PDS}&did={this.DID}&collection={this.Collection}&rkey={this.RecordKey}"
        member this.ExternalUrl = $"/ATProto/RedirectTo?pds={this.PDS}&did={this.DID}&collection={this.Collection}&rkey={this.RecordKey}"
        member this.PostedAt = this.Timestamp
        member this.ProfileUrl = $"/ATProto/RedirectTo?pds={this.PDS}&did={this.DID}&collection={this.Collection}"
        member this.Thumbnails = seq {
            if not (isNull this.ThumbnailUrl) then {
                new IPostThumbnail with
                    member _.AltText = this.ThumbnailAltText
                    member _.Url = this.ThumbnailUrl
            }
        }
        member this.Usericon = this.FeedIconUrl
        member this.Username = this.FeedTitle
