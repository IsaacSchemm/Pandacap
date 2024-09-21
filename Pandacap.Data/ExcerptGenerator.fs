namespace Pandacap.Data

open FSharp.Data

/// Allows Pandacap to derive a plaintext excerpt from an HTML string.
module ExcerptGenerator =
    /// Derives a plaintext excerpt from a plain text string.
    let FromText (e: string) =
        if e.Length > 60
        then $"{e.Substring(0, 60)}..."
        else e

    /// Derives a plaintext excerpt from an HTML string.
    let FromHtml (html: string) =
        try
            (HtmlDocument.Parse html).Elements()
            |> Seq.map (fun h -> h.InnerText())
            |> Seq.tryHead
            |> Option.map FromText
        with _ ->
            None
