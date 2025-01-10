namespace Pandacap.Data

open System
open Pandacap.PlatformBadges

/// A thumbnail to be shown for a post in one of Pandacap's "paged" areas.
type IPostThumbnail =
    abstract member Url: string
    abstract member AltText: string

/// A post to be shown in one of Pandacap's "paged" areas, like the gallery or inbox, using the "List" Razor view.
type IPost =
    abstract member Badges: Badge seq
    abstract member DisplayTitle: string
    abstract member Id: string
    abstract member IsDismissable: bool
    abstract member LinkUrl: string
    abstract member ProfileUrl: string
    abstract member Thumbnails: IPostThumbnail seq
    abstract member Timestamp: DateTimeOffset
    abstract member Username: string
    abstract member Usericon: string
