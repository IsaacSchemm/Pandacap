namespace Pandacap.Data

open System
open System.ComponentModel.DataAnnotations
open Pandacap.PlatformBadges

type FurAffinityFavoriteUser() =
    member val Name = "" with get, set
    member val ProfileName = "" with get, set
    member val Url = "" with get, set
    [<Obsolete("Will be replaced with an avatar URL field")>] member val AvatarModifiedTime = nullDateTimeOffset with get, set

type FurAffinityFavorite() =
    [<Key>]
    member val Id = Guid.Empty with get, set

    member val SubmissionId = 0 with get, set
    member val Title = "" with get, set
    member val Thumbnail = "" with get, set
    member val Link = "" with get, set
    member val PostedBy = new FurAffinityFavoriteUser() with get, set
    member val PostedAt = DateTimeOffset.MinValue with get, set

    member val FavoritedAt = DateTimeOffset.MinValue with get, set
    member val HiddenAt = nullDateTimeOffset with get, set

    interface IFavorite with
        member this.HiddenAt
            with get () = this.HiddenAt
             and set value = this.HiddenAt <- value

        member this.FavoritedAt = this.FavoritedAt

    interface IPost with
        member _.Platform = FurAffinity
        member this.Url = this.Link
        member this.DisplayTitle = this.Title
        member this.Id = $"{this.Id}"
        member this.InternalUrl = this.Link
        member this.ExternalUrl = this.Link
        member this.PostedAt = this.PostedAt
        member this.ProfileUrl = this.PostedBy.Url
        member this.Thumbnails = [{
            new IPostThumbnail with
                member _.AltText = null
                member _.Url = this.Thumbnail
        }]
        member this.Usericon =
            let avatar_mtime =
                this.PostedBy.AvatarModifiedTime
                |> Option.ofNullable
                |> Option.defaultValue this.PostedAt
            $"https://a.furaffinity.net/{avatar_mtime.ToUnixTimeSeconds()}/{Uri.EscapeDataString(this.PostedBy.ProfileName)}.gif"
        member this.Username = this.PostedBy.Name
