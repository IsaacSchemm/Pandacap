namespace Pandacap.Data

open System
open System.ComponentModel.DataAnnotations
open Pandacap.Html
open Pandacap.Types

/// An image attachment to an ActivityPub post from a follow.
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
        member this.DisplayTitle =
            if not (String.IsNullOrWhiteSpace this.Name) then
                this.Name
            else if not (String.IsNullOrWhiteSpace this.Content) then
                TextConverter.FromHtml this.Content
            else
                this.ObjectId
        member this.LinkUrl = $"/RemotePosts?id={Uri.EscapeDataString(this.ObjectId)}"
        member this.ProfileUrl = this.CreatedBy
        member this.Badges = [
            match Uri.TryCreate(this.CreatedBy, UriKind.Absolute) with
            | true, uri -> { PostPlatform.GetBadge ActivityPub with Text = uri.Host }
            | false, _ -> ()
        ]
        member this.Timestamp = this.CreatedAt
        member this.ThumbnailUrls = this.Attachments |> Seq.map (fun a -> a.Url)
        member this.Usericon = this.Usericon
        member this.Username = this.Username
