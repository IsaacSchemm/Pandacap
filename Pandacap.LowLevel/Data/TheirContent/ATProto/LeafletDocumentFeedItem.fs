namespace Pandacap.Data

open System
open System.ComponentModel.DataAnnotations
open Pandacap.PlatformBadges

type LeafletDocumentFeedItemPublication() =
    member val DID = "" with get, set
    member val PDS = "" with get, set
    member val Name = "" with get, set
    member val BasePath = "" with get, set

type LeafletDocumentFeedItem() =
    [<Key>]
    member val CID = "" with get, set

    member val RecordKey = "" with get, set
    member val Publication = new LeafletDocumentFeedItemPublication() with get, set
    member val CreatedAt = DateTimeOffset.MinValue with get, set
    member val Title = "" with get, set
    member val DismissedAt = nullDateTimeOffset with get, set

    interface IInboxPost with
        member this.DismissedAt
            with get () = this.DismissedAt
             and set value = this.DismissedAt <- value
        member _.IsPodcast = false
        member _.IsShare = false

    interface IPost with
        member _.Platform = Leaflet
        member this.Url = $"https://{this.Publication.PDS}"
        member this.DisplayTitle = this.Title
        member this.Id = $"{this.CID}"
        member this.InternalUrl = $"https://{this.Publication.BasePath}/{this.RecordKey}"
        member this.ExternalUrl = $"https://{this.Publication.BasePath}/{this.RecordKey}"
        member this.PostedAt = this.CreatedAt
        member this.ProfileUrl = $"https://{this.Publication.BasePath}"
        member _.Thumbnails = []
        member _.Usericon = null
        member this.Username = this.Publication.Name
