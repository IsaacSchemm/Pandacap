namespace Pandacap.Data

open System.ComponentModel.DataAnnotations

type StarpassPost() =
    [<Key>]
    member val FavoriteId = "" with get, set

    member val BlueskyDID = nullString with get, set
    member val BlueskyRecordKey = nullString with get, set
