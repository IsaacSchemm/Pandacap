namespace Pandacap.Html

open FSharp.Data

module Scraper =
    let GetOpenGraphMetadata(html: string) = dict [
        let document = HtmlDocument.Parse(html)
        for meta in document.CssSelect("meta") do
            let property = meta.TryGetAttribute("property")
            let content = meta.TryGetAttribute("content")
            match property, content with
            | Some p, Some c when p.Value().StartsWith("og:") ->
                p.Value(), c.Value()
            | _ -> ()
    ]

    let GetTitleFromHtml(html: string) =
        let document = HtmlDocument.Parse(html)
        try
            document.Html().CssSelect("title")
            |> Seq.map (fun node -> node.InnerText())
            |> Seq.tryHead
            |> Option.toObj
        with _ ->
            null
