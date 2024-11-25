namespace Pandacap.Data

open System

/// Allows Pandacap to derive a plaintext excerpt.
module ExcerptGenerator =
    /// Derives a plaintext excerpt from the first non-empty, non-whitespace string in a list of candidate strings.
    let FromText (length: int) (strings: string seq) =
        strings
        |> Seq.where (not << String.IsNullOrWhiteSpace)
        |> Seq.map (fun e ->
            if e.Length > length
            then $"{e.Substring(0, length)}..."
            else e)
        |> Seq.head
