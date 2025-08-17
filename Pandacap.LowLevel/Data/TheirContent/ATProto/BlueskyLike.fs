namespace Pandacap.Data

open System
open System.ComponentModel.DataAnnotations

type BlueskyLike() =
    [<Key>]
    member val Id = Guid.Empty with get, set

    member val DID = "" with get, set
    member val SubjectCID = "" with get, set
    member val SubjectRecordKey = "" with get, set
    member val LikeCID = "" with get, set
    member val LikeRecordKey = "" with get, set
