namespace Pandacap.Data

open System

type IFavorite =
    inherit IPost

    abstract member HiddenAt: Nullable<DateTimeOffset>
