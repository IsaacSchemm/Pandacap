namespace Pandacap.Data

open System
open System.ComponentModel.DataAnnotations
open Pandacap.PlatformBadges

type SheezyArtFavorite() =
    [<Key>]
    member val Id = Guid.Empty with get, set

    member val Title = "" with get, set
    member val Artist = "" with get, set
    member val Thumbnail = "" with get, set
    member val Avatar = "" with get, set
    member val Url = "" with get, set
    member val ProfileUrl = "" with get, set

    member val FavoritedAt = DateTimeOffset.MinValue with get, set
    member val HiddenAt = nullDateTimeOffset with get, set

    interface IFavorite with
        member this.HiddenAt
            with get () = this.HiddenAt
             and set value = this.HiddenAt <- value

        member this.FavoritedAt = this.FavoritedAt

    interface IPost with
        member _.Platform = SheezyArt
        member this.Url = this.Url
        member this.DisplayTitle = this.Title
        member this.Id = $"{this.Id}"
        member this.LinkUrl = this.Url
        member this.ProfileUrl = this.ProfileUrl
        member this.Thumbnails = [{
            new IPostThumbnail with
                member _.AltText = null
                member _.Url = this.Thumbnail
        }]
        member this.PostedAt = this.FavoritedAt
        member this.Usericon = this.Avatar
        member this.Username = this.Artist
