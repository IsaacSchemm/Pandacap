namespace Pandacap.Data

open System.ComponentModel.DataAnnotations

/// The active credentials for Fur Affinity.
type FurAffinityCredentials() =
    [<Key>]
    member val Username = "" with get, set
    member val A = "" with get, set
    member val B = "" with get, set

    interface FurAffinityFs.FurAffinity.ICredentials with
        member this.A = this.A
        member this.B = this.B
