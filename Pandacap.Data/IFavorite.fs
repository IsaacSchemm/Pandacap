namespace Pandacap.Data

open System

type IFavorite =
    inherit IPost

    abstract member FavoritedAt: DateTimeOffset with get
    abstract member HiddenAt: Nullable<DateTimeOffset> with get, set
