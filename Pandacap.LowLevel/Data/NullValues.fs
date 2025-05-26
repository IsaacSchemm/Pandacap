namespace Pandacap.Data

open System

[<AutoOpen>]
module internal NullValues =
    let nullString: string = null

    let nullDateTimeOffset = new Nullable<DateTimeOffset>()

    let orString (str2: string) (str1: string) =
        if isNull str1
        then str2
        else str1
