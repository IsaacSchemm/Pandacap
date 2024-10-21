namespace Pandacap.Data

open System
open System.ComponentModel.DataAnnotations.Schema
open Pandacap.Html

type PostType =
| StatusUpdate = 0
| JournalEntry = 1
| Artwork = 2

type PostBlobRef() =
    member val Id = Guid.Empty with get, set
    member val ContentType = "application/octet-stream" with get, set

type PostImage() =
    member val Blob = new PostBlobRef() with get, set
    member val Thumbnails = new ResizeArray<PostBlobRef>() with get, set
    member val AltText = nullString with get, set

    [<NotMapped>]
    member this.Thumbnail =
        this.Thumbnails
        |> Seq.tryHead
        |> Option.defaultValue this.Blob

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

    [<NotMapped>]
    member this.Thumbnails =
        this.Images
        |> Seq.map (fun image -> image.Thumbnail)
        |> Seq.truncate 1

    interface IPost with
        member this.DisplayTitle =
            seq {
                if this.Type <> PostType.StatusUpdate then
                    this.Title
                this.Body
                $"{this.Id}"
            }
            |> Seq.where (not << String.IsNullOrEmpty)
            |> Seq.head
        member this.Id = $"{this.Id}"
        member this.LinkUrl = $"/UserPosts/{this.Id}"
        member _.Badges = []
        member _.ProfileUrl = null
        member this.Timestamp = this.PublishedTime
        member this.ThumbnailUrls = seq {
            for thumb in this.Thumbnails do
                $"/Blobs/UserPosts/{this.Id}/{thumb.Id}"
        }
        member _.Usericon = null
        member _.Username = null
