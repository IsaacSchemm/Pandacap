namespace Pandacap.LowLevel

open System.Runtime.CompilerServices
open Pandacap.Data

[<Extension>]
module Ghosting =
    let private actors = seq {
        "https://bsky.brid.gy/bsky.brid.gy"
    }

    [<Extension>]
    let IsGhosted (follower: Follower) =
        actors |> Seq.contains follower.ActorId
