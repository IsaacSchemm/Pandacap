namespace Pandacap.Data

open System
open System.ComponentModel.DataAnnotations
open Pandacap.PlatformBadges

type FurryNetworkFavorite() =
    [<Key>]
    member val Id = Guid.Empty with get, set

    member val Title = "" with get, set
    member val Url = "" with get, set
    member val CreatorName = "" with get, set
    member val CreatorDisplayName = "" with get, set
    member val CreatorAvatarUrl = nullString with get, set
    member val ThumbnailUrl = nullString with get, set

    member val FavoritedAt = DateTimeOffset.MinValue with get, set
    member val HiddenAt = nullDateTimeOffset with get, set

    interface IFavorite with
        member this.HiddenAt
            with get () = this.HiddenAt
             and set value = this.HiddenAt <- value

        member this.FavoritedAt = this.FavoritedAt

    interface IPost with
        member _.Badges = [{ PostPlatform.GetBadge FurryNetwork with Text = "furrynetwork.com" }]
        member this.DisplayTitle = this.Title
        member this.Id = $"{this.Id}"
        member this.LinkUrl = this.Url
        member this.PostedAt = this.FavoritedAt
        member this.ProfileUrl = $"https://furrynetwork.com/${Uri.EscapeDataString(this.CreatorName)}"
        member this.Thumbnails = [
            if not (isNull this.ThumbnailUrl) then {
                new IPostThumbnail with
                    member _.AltText = null
                    member _.Url = this.ThumbnailUrl
            }
        ]
        member this.Usericon = this.CreatorAvatarUrl
        member this.Username = this.CreatorName
