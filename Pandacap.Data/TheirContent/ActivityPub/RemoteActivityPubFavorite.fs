namespace Pandacap.Data

open System
open System.ComponentModel.DataAnnotations
open Pandacap.Html
open Pandacap.PlatformBadges

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
    member val InReplyTo = nullString with get, set
    member val Attachments = new ResizeArray<RemoteActivityPubFavoriteImage>() with get, set

    interface IPost with
        member this.Badges = [
            match Uri.TryCreate(this.CreatedBy, UriKind.Absolute) with
            | true, uri -> PostPlatform.GetBadge ActivityPub |> Badge.WithParenthetical uri.Host
            | false, _ -> PostPlatform.GetBadge ActivityPub
        ]
        member this.DisplayTitle = ExcerptGenerator.FromText 120 [
            this.Name
            TextConverter.FromHtml this.Content
            for attachment in this.Attachments do
                attachment.Name
            this.ObjectId
        ]
        member this.Id = $"{this.LikeGuid}"
        member _.IsDismissable = false
        member this.LinkUrl = $"/RemotePosts?id={Uri.EscapeDataString(this.ObjectId)}"
        member this.ProfileUrl = this.CreatedBy
        member this.Thumbnails = [
            if not this.Sensitive then
                for a in this.Attachments do {
                    new IPostThumbnail with
                        member _.AltText = a.Name
                        member _.Url = a.Url
                }
        ]
        member this.Timestamp = this.CreatedAt
        member this.Usericon = this.Usericon
        member this.Username = this.Username

    interface Pandacap.ActivityPub.ILike with
        member this.ObjectId = this.ObjectId
