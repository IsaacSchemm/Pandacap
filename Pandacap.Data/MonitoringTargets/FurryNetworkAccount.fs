namespace Pandacap.Data

open System.ComponentModel.DataAnnotations

/// An account on FN.
type FurryNetworkAccount() =
    [<Key>]
    member val Username = "" with get, set
