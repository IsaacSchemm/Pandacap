﻿namespace Pandacap.Data

open System
open System.ComponentModel.DataAnnotations.Schema

[<AllowNullLiteral>]
type UserPostBlobReference() =
    member val Id = Guid.Empty with get, set
    member val ContentType = "application/octet-stream" with get, set

    /// The name of the blob in Azure Storage that contains the data.
    [<NotMapped>]
    member this.BlobName = $"{this.Id}"

/// A Pandacap post imported from this instance's owner's DeviantArt account.
type UserPost() =
    member val Id = Guid.Empty with get, set
    member val Title = nullString with get, set
    member val Artwork = false with get, set
    member val Image: UserPostBlobReference = null with get, set
    member val Thumbnail: UserPostBlobReference = null with get, set
    member val IsMature = false with get, set
    member val Description = nullString with get, set
    member val Tags = new ResizeArray<string>() with get, set
    member val PublishedTime = DateTimeOffset.MinValue with get, set
    member val Url = nullString with get, set

    member val AltText = nullString with get, set
    member val HideTitle = false with get, set
    member val IsArticle = false with get, set

    [<NotMapped>]
    member this.BlobReferences = List.choose Option.ofObj [this.Image; this.Thumbnail]

    interface IPost with
        member this.DisplayTitle = this.Title |> orString $"{this.Id}"
        member this.Id = $"{this.Id}"
        member this.Images = seq {
            if not (Seq.isEmpty this.BlobReferences) then {
                new IPostImage with
                    member _.AltText = this.AltText
                    member _.ThumbnailUrl = $"/Blobs/Thumbnails/{this.Id}"
            }
        }
        member this.LinkUrl = $"/UserPosts/{this.Id}"
        member this.Timestamp = this.PublishedTime
        member _.Usericon = null
        member _.Username = null
