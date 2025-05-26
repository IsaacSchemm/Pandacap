namespace Pandacap.Html

open System

/// Allows Pandacap to derive a plaintext excerpt.
module ExcerptGenerator =
    let FromText (length: int) (e: string) =
        if isNull e then ""
        else if e.Length > length then $"{e.Substring(0, length)}..."
        else e

    let FromFirst (length: int) (strings: string seq) =
        strings
        |> Seq.where (not << String.IsNullOrWhiteSpace)
        |> Seq.map (FromText length)
        |> Seq.tryHead
        |> Option.defaultValue ""
