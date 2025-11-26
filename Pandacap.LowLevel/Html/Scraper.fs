namespace Pandacap.Html

open FSharp.Data

module Scraper =
    let GetTitleFromHtml(html: string) =
        let document = HtmlDocument.Parse(html)
        try
            document.Html().CssSelect("title")
            |> Seq.map (fun node -> node.InnerText())
            |> Seq.tryHead
            |> Option.toObj
        with _ ->
            null
