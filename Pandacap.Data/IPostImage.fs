namespace Pandacap.Data

/// A thumbnail for a post shown in one of the "paged" areas.
type IPostImage =
    abstract member ThumbnailUrl: string
    abstract member AltText: string
