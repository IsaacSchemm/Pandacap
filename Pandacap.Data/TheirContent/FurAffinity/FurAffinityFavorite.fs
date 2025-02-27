namespace Pandacap.Data

open System
open System.ComponentModel.DataAnnotations
open Pandacap.PlatformBadges

type FurAffinityFavoriteUser() =
    member val Name = "" with get, set
    member val ProfileName = "" with get, set
    member val Url = "" with get, set

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
        member this.HiddenAt = this.HiddenAt

    interface IPost with
        member _.Badges = [{ PostPlatform.GetBadge FurAffinity with Text = "furaffinity.net" }]
        member this.DisplayTitle = this.Title
        member this.Id = $"{this.Id}"
        member _.IsDismissable = true
        member this.LinkUrl = this.Link
        member this.ProfileUrl = this.PostedBy.Url
        member this.Thumbnails = [{
            new IPostThumbnail with
                member _.AltText = null
                member _.Url = this.Thumbnail
        }]
        member this.Timestamp = this.FavoritedAt
        member this.Usericon = $"https://a.furaffinity.net/{this.PostedAt.ToUnixTimeSeconds()}/{Uri.EscapeDataString(this.PostedBy.ProfileName)}.gif"
        member this.Username = this.PostedBy.Name
