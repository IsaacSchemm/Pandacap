namespace Pandacap.Data

open FSharp.Data

/// Allows Pandacap to convert HTML to plain text when possible.
module internal TextConverter =
    let FromHtml content =
        try
            String.concat "\n" [
                let doc = HtmlDocument.Parse $"<div>{content}</div>"
                for node in doc.Elements() do
                    node.InnerText()
            ]
        with _ ->
            content
