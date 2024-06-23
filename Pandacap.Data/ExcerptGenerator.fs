namespace Pandacap.Data

open FSharp.Data

/// Allows Pandacap to derive a plaintext excerpt from an HTML string.
module internal ExcerptGenerator =
    /// Derives a plaintext excerpt from an HTML string.
    let compute (html: string) =
        (HtmlDocument.Parse html).Elements()
        |> Seq.map (fun h -> h.InnerText())
        |> Seq.tryHead
        |> Option.map (fun e ->
            if e.Length > 60
            then $"{e.Substring(0, 60)}..."
            else e)
