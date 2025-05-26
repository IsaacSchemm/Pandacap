namespace Pandacap.Html

open FSharp.Data

/// Allows Pandacap to convert HTML to plain text when possible.
module TextConverter =
    let FromHtml content =
        let rec getText (node: HtmlNode) = String.concat " " [
            match node.Elements() with
            | [] -> node.InnerText()
            | list -> for child in list do getText child
        ]

        try
            String.concat " " [
                let doc = HtmlDocument.Parse $"<body>{content}</body>"
                getText (doc.Body())
            ]
        with _ ->
            content
