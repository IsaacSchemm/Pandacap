namespace Pandacap.Data

open System
open System.ComponentModel.DataAnnotations.Schema

type PostType =
| StatusUpdate = 0
| JournalEntry = 1
| Artwork = 2

type PostBlobRef() =
    member val Id = Guid.Empty with get, set
    member val ContentType = "application/octet-stream" with get, set

[<AllowNullLiteral>]
type PostImageFocalPoint() =
    member val Horizontal = 0m with get, set
    member val Vertical = 0m with get, set

type PostImage() =
    member val Blob = new PostBlobRef() with get, set
    member val Thumbnails = new ResizeArray<PostBlobRef>() with get, set
    member val AltText = nullString with get, set

    member val FocalPoint: PostImageFocalPoint = null with get, set

    [<NotMapped>]
    member this.Thumbnail =
        this.Thumbnails
        |> Seq.tryHead
        |> Option.defaultValue this.Blob

    interface Pandacap.ActivityPub.IImage with
        member this.BlobId = this.Blob.Id
        member this.HorizontalFocalPoint =
            this.FocalPoint
            |> Option.ofObj
            |> Option.map (fun f -> f.Horizontal)
        member this.MediaType = this.Blob.ContentType
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
    member this.Html =
        if isNull this.Body then null
        else CommonMark.CommonMarkConverter.Convert this.Body

    [<NotMapped>]
    member this.Blobs = seq {
        for i in this.Images do
            yield i.Blob
            yield! i.Thumbnails
    }

    interface IPost with
        member _.Badges = []
        member this.DisplayTitle =
            match this.Type with
            | PostType.StatusUpdate -> "Status update"
            | _ -> this.Title
        member this.Id = $"{this.Id}"
        member this.LinkUrl = $"/UserPosts/{this.Id}"
        member _.ProfileUrl = null
        member this.Timestamp = this.PublishedTime
        member this.Thumbnails = seq {
            for image in this.Images do {
                new IPostThumbnail with
                    member _.AltText = image.AltText
                    member _.Url = $"/Blobs/UserPosts/{this.Id}/{image.Thumbnail.Id}"
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
