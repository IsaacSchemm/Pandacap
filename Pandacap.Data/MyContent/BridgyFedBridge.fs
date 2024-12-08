namespace Pandacap.Data

open System.ComponentModel.DataAnnotations

type BridgyFedBridge() =
    [<Key>]
    member val DID = "" with get, set
