namespace Pandacap.Data

open System

type IInboxPost =
    inherit IPost

    abstract member DismissedAt: Nullable<DateTimeOffset>
