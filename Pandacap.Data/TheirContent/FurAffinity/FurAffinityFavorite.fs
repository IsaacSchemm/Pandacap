namespace Pandacap.Data

open System
open System.ComponentModel.DataAnnotations
open Pandacap.PlatformBadges

type FurAffinityFavoriteUser() =
    member val Name = "" with get, set
    member val Url = "" with get, set

type FurAffinityFavorite() =
    [<Key>]
    member val Id = Guid.Empty with get, set

    member val SubmissionId = 0 with get, set
    member val Title = "" with get, set
    member val Thumbnail = "" with get, set
    member val Link = "" with get, set
    member val PostedBy = new InboxFurAffinitySubmissionUser() with get, set
    member val PostedAt = DateTimeOffset.MinValue with get, set

    member val FavoritedAt = DateTimeOffset.MinValue with get, set

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
        member this.Timestamp = this.PostedAt
        member _.Usericon = null
        member this.Username = this.PostedBy.Name
