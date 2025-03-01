namespace Pandacap.Data

open System.ComponentModel.DataAnnotations

/// An account on Sheezy.Art.
type SheezyArtAccount() =
    [<Key>]
    member val Username = "" with get, set
