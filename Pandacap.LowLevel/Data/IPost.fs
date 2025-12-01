namespace Pandacap.Data

open System
open Pandacap.PlatformBadges

/// A thumbnail to be shown for a post in one of Pandacap's "paged" areas.
type IPostThumbnail =
    abstract member Url: string
    abstract member AltText: string

module PostThumbnail =
    let Empty = {
        new IPostThumbnail with
            member _.Url = "data:image/svg+xml,<svg xmlns=\"http://www.w3.org/2000/svg\"/>"
            member _.AltText = ""
    }

type IPostAuthor =
    abstract member ProfileUrl: string
    abstract member Username: string
    abstract member Usericon: string

/// A post to be shown in one of Pandacap's "paged" areas, like the gallery or inbox, using the "List" Razor view.
type IPost =
    inherit IPostAuthor

    abstract member Platform: PostPlatform
    abstract member Url: string
    abstract member DisplayTitle: string
    abstract member Id: string
    abstract member InternalUrl: string
    abstract member ExternalUrl: string
    abstract member PostedAt: DateTimeOffset
    abstract member Thumbnails: IPostThumbnail seq
