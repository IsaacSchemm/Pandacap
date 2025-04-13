namespace Pandacap.Data

open System
open System.ComponentModel.DataAnnotations
open Pandacap.PlatformBadges

type RedditUpvotedPost() =
    [<Key>]
    member val Id = Guid.Empty with get, set

    member val Id36 = "" with get, set
    member val Author = nullString with get, set
    member val Created = nullDateTimeOffset with get, set
    member val Title = "" with get, set
    member val Thumbnail = nullString with get, set
    member val URL = "" with get, set

    member val FavoritedAt = DateTimeOffset.MinValue with get, set
    member val HiddenAt = nullDateTimeOffset with get, set

    interface IFavorite with
        member this.HiddenAt
            with get () = this.HiddenAt
             and set value = this.HiddenAt <- value

        member this.PostedAt = if this.Created.HasValue then this.Created.Value else DateTimeOffset.MinValue

    interface IPost with
        member _.Badges = [{ PostPlatform.GetBadge Reddit with Text = "reddit.com" }]
        member this.DisplayTitle = this.Title
        member this.Id = $"{this.Id}"
        member this.LinkUrl = this.URL
        member this.ProfileUrl = $"https://www.reddit.com/user/{Uri.EscapeDataString(this.Author)}"
        member this.Thumbnails = [
            if not (isNull this.Thumbnail) then {
                new IPostThumbnail with
                    member _.AltText = null
                    member _.Url = this.Thumbnail
            }
        ]
        member this.Timestamp = this.FavoritedAt
        member _.Usericon = null
        member this.Username = this.Author
