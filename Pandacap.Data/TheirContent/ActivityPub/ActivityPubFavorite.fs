namespace Pandacap.Data

open System
open Pandacap.Html
open Pandacap.PlatformBadges

/// An image attachment to an ActivityPub post.
type ActivityPubFavoriteImage() =
    member val Url = "" with get, set
    member val Name = nullString with get, set

/// A remote ActivityPub post that this app's instance owner has liked or reposted.
[<AbstractClass>]
type ActivityPubFavorite() =
    abstract member Id: Guid with get

    member val ObjectId = "" with get, set
    member val CreatedBy = "" with get, set
    member val Username = nullString with get, set
    member val Usericon = nullString with get, set
    member val CreatedAt = DateTimeOffset.MinValue with get, set
    member val FavoritedAt = DateTimeOffset.MinValue with get, set
    member val HiddenAt = nullDateTimeOffset with get, set
    member val Summary = nullString with get, set
    member val Sensitive = false with get, set
    member val Name = nullString with get, set
    member val Content = nullString with get, set
    member val InReplyTo = nullString with get, set
    member val Attachments = new ResizeArray<ActivityPubFavoriteImage>() with get, set

    interface IFavorite with
        member this.HiddenAt
            with get () = this.HiddenAt
             and set value = this.HiddenAt <- value

        member this.FavoritedAt = this.FavoritedAt

    interface IPost with
        member this.Badges = [
            match Uri.TryCreate(this.CreatedBy, UriKind.Absolute) with
            | true, uri -> { PostPlatform.GetBadge ActivityPub with Text = uri.Host }
            | false, _ -> PostPlatform.GetBadge ActivityPub
        ]
        member this.DisplayTitle =
            if not (String.IsNullOrWhiteSpace(this.Name)) then
                this.Name
            else
                this.Content
                |> TextConverter.FromHtml
                |> TitleGenerator.FromBody
        member this.Id = $"{this.Id}"
        member this.LinkUrl = $"/RemotePosts?id={Uri.EscapeDataString(this.ObjectId)}"
        member this.PostedAt = this.CreatedAt
        member this.ProfileUrl = this.CreatedBy
        member this.Thumbnails = [
            if not this.Sensitive then
                for a in this.Attachments do
                    if not (String.IsNullOrEmpty(a.Url)) then {
                        new IPostThumbnail with
                            member _.AltText = a.Name
                            member _.Url = a.Url
                    }
        ]
        member this.Usericon = this.Usericon
        member this.Username = this.Username
