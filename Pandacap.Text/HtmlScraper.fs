namespace Pandacap.Text

open FSharp.Data

module HtmlScraper =
    let private tryGetAttributeValue (name: string) (node: HtmlNode) =
        node.TryGetAttribute(name) |> Option.map (fun a -> a.Value())

    let GetOpenGraphMetadata(html) = dict [
        let document = HtmlDocument.Parse(html)
        for meta in document.CssSelect("meta") do
            let property = meta.TryGetAttribute("property")
            let content = meta.TryGetAttribute("content")
            match property, content with
            | Some p, Some c when p.Value().StartsWith("og:") ->
                p.Value(), c.Value()
            | _ -> ()
    ]

    let GetTitle(html) =
        let document = HtmlDocument.Parse(html)
        try
            document.Html().CssSelect("title")
            |> Seq.map (fun node -> node.InnerText())
            |> Seq.tryHead
            |> Option.toObj
        with _ ->
            null

    let FindImages(html) =
        try
            let doc = HtmlDocument.Parse(html)
            seq {
                for node in doc.Descendants("img") do
                    match tryGetAttributeValue "src" node with
                    | None -> ()
                    | Some src -> {|
                        altText =
                            node
                            |> tryGetAttributeValue "alt"
                            |> Option.defaultValue ""
                        url = src
                    |}
            }
        with _ -> Seq.empty

    let FindFavicons(html) = 
        try
            let doc = HtmlDocument.Parse(html)

            doc.Descendants("link")
            |> Seq.choose (fun node ->
                match tryGetAttributeValue "rel" node with
                | Some "icon" -> tryGetAttributeValue "href" node
                | _ -> None)
        with _ -> Seq.empty

    let FindAlternateLinks(html) = seq {
        let document = HtmlDocument.Parse html
        let links = document.CssSelect("link")
        for link in links do
            let attr x =
                link.TryGetAttribute(x)
                |> Option.map (fun a -> a.Value())
            if attr "rel" = Some "alternate" then
                match attr "type", attr "href" with
                | Some t, Some href ->
                    yield {|
                        Type = t
                        Href = href
                    |}
                | _ -> ()
    }
