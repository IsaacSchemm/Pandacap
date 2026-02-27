namespace Pandacap.Html

open FSharp.Data

module ImageFinder =
    let private tryGetAttributeValue (name: string) (node: HtmlNode) =
        node.TryGetAttribute(name) |> Option.map (fun a -> a.Value())

    let FindImagesInHTML html =
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

    let FindFaviconsInHTML html = 
        try
            let doc = HtmlDocument.Parse(html)

            doc.Descendants("link")
            |> Seq.choose (fun node ->
                match tryGetAttributeValue "rel" node with
                | Some "icon" -> tryGetAttributeValue "href" node
                | _ -> None)
        with _ -> Seq.empty
