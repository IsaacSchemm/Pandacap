namespace Pandacap.Data

open System

/// A thumbnail for a post shown in one of the "paged" areas.
type IPostImage =
    abstract member ThumbnailUrl: string
    abstract member AltText: string

/// The username and icon/avatar associated with a displayed post.
type IPostCreator =
    abstract member Username: string
    abstract member Usericon: string

/// A post to be shown in one of Pandacap's "paged" areas, like the gallery or inbox, using the "List" Razor view.
type IPost =
    abstract member Id: string
    abstract member CreatedBy: IPostCreator
    abstract member DisplayTitle: string
    abstract member Timestamp: DateTimeOffset
    abstract member LinkUrl: string
    abstract member Images: IPostImage seq 
