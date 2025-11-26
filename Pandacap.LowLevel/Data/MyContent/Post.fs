namespace Pandacap.Data

open System
open System.ComponentModel.DataAnnotations.Schema
open System.Net
open Pandacap.PlatformBadges

type PostType =
| StatusUpdate = 0
| JournalEntry = 1
| Artwork = 2
| Scraps = 3
| Link = 4

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

type PostLink() =
    member val Title = "" with get, set
    member val ContentType = "" with get, set
    member val Url = "" with get, set

type Post() =
    member val Id = Guid.Empty with get, set

    member val Type = PostType.StatusUpdate with get, set

    member val Title = nullString with get, set
    member val Body = nullString with get, set
    member val Images = new ResizeArray<PostImage>() with get, set
    member val Links = new ResizeArray<PostLink>() with get, set
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
        member this.InternalUrl = $"/UserPosts/{this.Id}"
        member this.ExternalUrl = $"/UserPosts/{this.Id}"
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
        member this.GetObjectId(hostInfo) = $"https://{hostInfo.ApplicationHostname}/UserPosts/{this.Id}"
        member _.GetAddressing(hostInfo) = {
            new Pandacap.ActivityPub.IAddressing with
                member _.InReplyTo = null
                member _.To = ["https://www.w3.org/ns/activitystreams#Public"]
                member _.Cc = [hostInfo.ActivityPubFollowersRootId]
                member _.Audience = null
        }
        member this.Html = String.concat "" [
            this.Html

            if not (isNull this.Links) then
                for link in this.Links do
                    $"""<p><a href="{link.Url}" target="_blank">{WebUtility.HtmlEncode(link.Url)}</a></p>"""
        ]
        member this.Links = seq {
            if not (isNull this.Links) then
                for link in this.Links do {
                    new Pandacap.ActivityPub.ILink with
                        member _.Href = link.Url
                        member _.MediaType = link.ContentType
                }
        }
        member this.Images = seq {
            if not (isNull this.Images) then
                for image in this.Images do {
                    new Pandacap.ActivityPub.IImage with
                        member _.GetUrl(appInfo) = $"https://{appInfo.ApplicationHostname}/Blobs/UserPosts/{this.Id}/{image.Raster.Id}"
                        member _.HorizontalFocalPoint =
                            image.FocalPoint
                            |> Option.ofObj
                            |> Option.map (fun f -> f.Horizontal)
                        member _.MediaType = image.Raster.ContentType
                        member _.VerticalFocalPoint =
                            image.FocalPoint
                            |> Option.ofObj
                            |> Option.map (fun f -> f.Vertical)
                        member _.AltText = image.AltText
                }
        }
        member this.IsJournal = this.Type = PostType.JournalEntry
        member this.PublishedTime = this.PublishedTime
        member this.Tags = this.Tags
        member this.Title = this.Title
