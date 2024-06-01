namespace Pandacap.LowLevel

open System

type FeedReaderScope =
| AtOrSince of DateTimeOffset
| AllTime
with
    member this.IsInScope(timestamp: DateTimeOffset option) =
        match timestamp, this with
        | Some ts, AtOrSince cutoff -> ts >= cutoff
        | _ -> true
