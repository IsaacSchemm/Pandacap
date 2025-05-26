namespace Pandacap.Html

open FSharp.Data

module LinkRelAlternate =
    let ParseFromHtml (html: string) = seq {
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
