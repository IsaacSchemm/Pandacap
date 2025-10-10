namespace Pandacap.Html

open FSharp.Data

module ImageFinder =
    let FindImagesInHTML html =
        try
            let doc = HtmlDocument.Parse(html)
            seq {
                for node in doc.Descendants("img") do
                    match node.TryGetAttribute("src") with
                    | None -> ()
                    | Some srcAttr -> {|
                        altText =
                            node.TryGetAttribute("alt")
                            |> Option.map (fun attr -> attr.Value())
                            |> Option.defaultValue ""
                        url = srcAttr.Value()
                    |}
            }
        with _ -> Seq.empty
