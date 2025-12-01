namespace Pandacap.Data

open System

type IInboxPost =
    inherit IPost

    abstract member DismissedAt: Nullable<DateTimeOffset> with get, set
    abstract member IsPodcast: bool
    abstract member IsShare: bool
