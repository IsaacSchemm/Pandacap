namespace Pandacap.Data

open System
open Pandacap.PlatformBadges

type Upload() =
    member val Id = Guid.Empty with get, set
    member val ContentType = "application/octet-stream" with get, set
    member val Raster = Nullable<Guid>() with get, set
    member val AltText = "" with get, set
    member val UploadedAt = DateTimeOffset.MinValue with get, set

    interface IPost with
        member _.Platform = Pandacap
        member _.Url = null
        member this.DisplayTitle = String.concat " " [
            this.UploadedAt.UtcDateTime.ToLongDateString()
            this.UploadedAt.UtcDateTime.ToShortTimeString()
        ]
        member this.Id = $"{this.Id}"
        member this.LinkUrl = $"/Uploads/{this.Id}"
        member this.PostedAt = this.UploadedAt
        member _.ProfileUrl = null
        member this.Thumbnails = [{
            new IPostThumbnail with
                member _.AltText = this.AltText
                member _.Url = $"/Blobs/Uploads/{this.Id}"
        }]
        member _.Usericon = null
        member _.Username = null
