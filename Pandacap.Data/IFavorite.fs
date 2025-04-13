namespace Pandacap.Data

open System

type IFavorite =
    inherit IPost

    abstract member HiddenAt: Nullable<DateTimeOffset> with get, set

    abstract member PostedAt: DateTimeOffset with get
