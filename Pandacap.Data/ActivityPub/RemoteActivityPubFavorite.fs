namespace Pandacap.Data

open System
open System.ComponentModel.DataAnnotations

/// An image attachment to a remote favorite ActivityPub post.
type RemoteActivityPubFavoriteImage() =
    member val Url = "" with get, set
    member val Name = nullString with get, set

/// A remote ActivityPub post that this app's instance owner has added to their Favorites.
type RemoteActivityPubFavorite() =
    [<Key>]
    member val LikeGuid = Guid.Empty with get, set

    member val ObjectId = "" with get, set
    member val CreatedBy = "" with get, set
    member val Username = nullString with get, set
    member val Usericon = nullString with get, set
    member val CreatedAt = DateTimeOffset.MinValue with get, set
    member val FavoritedAt = DateTimeOffset.MinValue with get, set
    member val Summary = nullString with get, set
    member val Sensitive = false with get, set
    member val Name = nullString with get, set
    member val Content = nullString with get, set
    member val Attachments = new ResizeArray<RemoteActivityPubFavoriteImage>() with get, set

    interface IPost with
        member this.Id = $"{this.LikeGuid}"
        member this.Username = this.Username
        member this.Usericon = this.Usericon
        member this.DisplayTitle =
            Option.ofObj this.Name
            |> Option.orElse (Excerpt.compute this.Content)
            |> Option.defaultValue $"{this.ObjectId}"
        member this.Timestamp = this.CreatedAt
        member this.LinkUrl = this.ObjectId
        member this.Images = seq {
            for image in this.Attachments do {
                new IPostImage with
                    member _.ThumbnailUrl = image.Url
                    member _.AltText = image.Name
            }
        }
