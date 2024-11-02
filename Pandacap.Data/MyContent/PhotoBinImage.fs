namespace Pandacap.Data

open System

type PhotoBinImage() =
    member val Id = Guid.Empty with get, set
    member val ContentType = "application/octet-stream" with get, set
    member val AltText = "" with get, set
    member val UploadedAt = DateTimeOffset.MinValue with get, set

    interface IPost with
        member _.Badges = []
        member this.DisplayTitle = String.concat " " [
            this.UploadedAt.Date.ToLongDateString()
            this.UploadedAt.Date.ToShortTimeString()
        ]
        member this.Id = $"{this.Id}"
        member _.IsDismissable = false
        member this.LinkUrl = $"/PhotoBin/ViewImage?id={this.Id}"
        member _.ProfileUrl = null
        member this.Thumbnails = [{
            new IPostThumbnail with
                member _.AltText = this.AltText
                member _.Url = $"/Blobs/PhotoBin/{this.Id}"
        }]
        member this.Timestamp = this.UploadedAt
        member _.Usericon = null
        member _.Username = null
