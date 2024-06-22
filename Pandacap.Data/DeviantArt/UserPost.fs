namespace Pandacap.Data

open System
open System.ComponentModel.DataAnnotations.Schema

/// A Pandacap post imported from this instance's owner's DeviantArt account.
type UserPost() =

    /// The DeviantArt ID for this post.
    member val Id = Guid.Empty with get, set

    /// The title of the post, if any.
    member val Title = nullString with get, set

    /// Whether this post is considered an artwork post and included in the Gallery section.
    member val Artwork = false with get, set

    /// The attached image, if any.
    /// If there is an image, it will be stored in an Azure Storage account, and proxied through ImagesController.
    member val Image: BlobReference = null with get, set

    /// A thumbnail for the attached image, if any.
    member val Thumbnail: BlobReference = null with get, set

    [<NotMapped>]
    member this.BlobReferences = List.choose Option.ofObj [this.Image; this.Thumbnail]

    /// Descriptive text for the contents of the image, if any.
    member val AltText = nullString with get, set

    /// Whether this post contains mature content.
    member val IsMature = false with get, set

    /// The HTML description of the post, if any.
    member val Description = nullString with get, set

    /// Tags attached to the post, if any.
    member val Tags = new ResizeArray<string>() with get, set

    /// The date and time at which this post was created.
    member val PublishedTime = DateTimeOffset.MinValue with get, set

    /// The URL to view this post on DeviantArt.
    member val Url = nullString with get, set

    /// Whether to hide the title of this post when displaying the full contents.
    member val HideTitle = false with get, set

    /// Whether this post should be rendered in ActivityPub as an Article (instead of a Note).
    member val IsArticle = false with get, set

    interface IPost with
        member this.DisplayTitle = this.Title |> orString $"{this.Id}"
        member this.Id = $"{this.Id}"
        member this.Images = seq {
            if not (isNull this.Thumbnail) then {
                new IPostImage with
                    member _.AltText = this.AltText
                    member _.ThumbnailUrl = $"/Blobs/Thumbnails/{this.Id}"
            }
        }
        member this.LinkUrl = $"/UserPosts/{this.Id}"
        member this.Timestamp = this.PublishedTime
        member _.Usericon = null
        member _.Username = null
