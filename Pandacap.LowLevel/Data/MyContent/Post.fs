namespace Pandacap.Data

open System
open System.ComponentModel.DataAnnotations.Schema
open Pandacap.PlatformBadges

type PostType =
| StatusUpdate = 0
| JournalEntry = 1
| Artwork = 2
| Scraps = 3

type PostBlobRef() =
    member val Id = Guid.Empty with get, set
    member val ContentType = "application/octet-stream" with get, set

    [<NotMapped>]
    member this.IsRaster =
        this.ContentType <> "image/svg+xml"

[<AllowNullLiteral>]
type PostImageFocalPoint() =
    member val Horizontal = 0m with get, set
    member val Vertical = 0m with get, set

type PostImage() =
    member val Renditions = new ResizeArray<PostBlobRef>() with get, set
    member val AltText = nullString with get, set

    member val FocalPoint: PostImageFocalPoint = null with get, set

    [<NotMapped>]
    member this.Primary =
        Seq.head this.Renditions

    [<NotMapped>]
    member this.PrimaryThumbnail =
        this.Renditions
        |> Seq.where (fun b -> not b.IsRaster)
        |> Seq.tryHead
        |> Option.defaultValue (Seq.last this.Renditions)

    [<NotMapped>]
    member this.Raster =
        this.Renditions
        |> Seq.where (fun b -> b.IsRaster)
        |> Seq.tryHead
        |> Option.defaultValue (Seq.head this.Renditions)

    interface Pandacap.ActivityPub.IImage with
        member this.BlobId = this.Raster.Id
        member this.HorizontalFocalPoint =
            this.FocalPoint
            |> Option.ofObj
            |> Option.map (fun f -> f.Horizontal)
        member this.MediaType = this.Raster.ContentType
        member this.VerticalFocalPoint =
            this.FocalPoint
            |> Option.ofObj
            |> Option.map (fun f -> f.Vertical)
        member this.AltText = this.AltText

type Post() =
    member val Id = Guid.Empty with get, set

    member val Type = PostType.StatusUpdate with get, set

    member val Title = nullString with get, set
    member val Body = nullString with get, set
    member val Images = new ResizeArray<PostImage>() with get, set
    member val Tags = new ResizeArray<string>() with get, set

    member val PublishedTime = DateTimeOffset.MinValue with get, set

    member val BlueskyDID = nullString with get, set
    member val BlueskyRecordKey = nullString with get, set

    member val DeviantArtId = Nullable<Guid>() with get, set
    member val DeviantArtUrl = nullString with get, set

    member val FurAffinitySubmissionId = Nullable<int>() with get, set
    member val FurAffinityJournalId = Nullable<int>() with get, set

    member val WeasylSubmitId = Nullable<int>() with get, set
    member val WeasylJournalId = Nullable<int>() with get, set

    [<NotMapped>]
    member this.IsTextPost =
        match this.Type with
        | PostType.JournalEntry
        | PostType.StatusUpdate -> true
        | _ -> false

    [<NotMapped>]
    member this.Html =
        if isNull this.Body then null
        else CommonMark.CommonMarkConverter.Convert this.Body

    [<NotMapped>]
    member this.Blobs =
        this.Images
        |> Seq.collect (fun i -> i.Renditions)

    interface IPost with
        member _.Platform = Pandacap
        member _.Url = null
        member this.DisplayTitle =
            match this.Type with
            | PostType.StatusUpdate -> "Status update"
            | _ -> this.Title
        member this.Id = $"{this.Id}"
        member this.LinkUrl = $"/UserPosts/{this.Id}"
        member this.PostedAt = this.PublishedTime
        member _.ProfileUrl = null
        member this.Thumbnails = seq {
            for image in this.Images do {
                new IPostThumbnail with
                    member _.AltText = image.AltText
                    member _.Url = $"/Blobs/UserPosts/{this.Id}/{image.PrimaryThumbnail.Id}"
            }
        }
        member _.Usericon = null
        member _.Username = null

    interface Pandacap.ActivityPub.IPost with
        member this.Html = this.Html
        member this.Id = this.Id
        member this.Images = [for image in this.Images do image]
        member this.IsJournal = this.Type = PostType.JournalEntry
        member this.PublishedTime = this.PublishedTime
        member this.Tags = this.Tags
        member this.Title = this.Title
