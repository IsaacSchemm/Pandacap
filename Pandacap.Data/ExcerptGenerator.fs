namespace Pandacap.Data

open System
open FSharp.Data

/// Allows Pandacap to derive a plaintext excerpt from an HTML string.
module ExcerptGenerator =
    /// Derives a plaintext excerpt from the first non-empty, non-whitespace string in the list.
    let FromText (strings: string seq) =
        strings
        |> Seq.where (not << String.IsNullOrWhiteSpace)
        |> Seq.map (fun e ->
            if e.Length > 60
            then $"{e.Substring(0, 60)}..."
            else e)
        |> Seq.head

    ///// Derives a plaintext excerpt from an HTML string.
    //let FromHtml (html: string) =
    //    try
    //        (HtmlDocument.Parse html).Elements()
    //        |> Seq.map (fun h -> h.InnerText())
    //        |> FromText
    //        |> Some
    //    with _ ->
    //        None
