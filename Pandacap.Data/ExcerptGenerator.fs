namespace Pandacap.Data

open FSharp.Data

/// Allows Pandacap to derive a plaintext excerpt from an HTML string.
module internal ExcerptGenerator =
    /// Derives a plaintext excerpt from a plain text string.
    let fromText (e: string) =
        if e.Length > 60
        then $"{e.Substring(0, 60)}..."
        else e

    /// Derives a plaintext excerpt from an HTML string.
    let fromHtml (html: string) =
        (HtmlDocument.Parse html).Elements()
        |> Seq.map (fun h -> h.InnerText())
        |> Seq.tryHead
        |> Option.map fromText
