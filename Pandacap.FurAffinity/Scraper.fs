module internal Scraper

open FSharp.Data
open Pandacap.FurAffinity.Models

let getName node =
    node
    |> HtmlNode.innerText

let getValue node =
    node
    |> HtmlNode.tryGetAttribute "value"
    |> Option.map (fun a -> HtmlAttribute.value a)
    |> Option.defaultValue (getName node)

let getDescendants node =
    HtmlNode.descendants false (fun _ -> true) node

let getPostOptions (selector: string) (document: HtmlDocument) = [
    for select in document.CssSelect selector do
        for x in getDescendants select do
            match (HtmlNode.name x).ToLowerInvariant() with
            | "option" ->
                { Value = getValue x; Name = getName x }
            | "optgroup" ->
                for y in getDescendants x do
                    { Value = getValue y; Name = getName y }
            | _ -> ()
]

let extractAuthenticityToken (action: string) (html: HtmlDocument) =
    let m =
        html.CssSelect($"form[action={action}] input[name=key]")
        |> Seq.map (fun e -> e.AttributeValue("value"))
        |> Seq.tryHead
    match m with
        | Some token -> token
        | None -> failwith $"Form with action \"{action}\" and hidden input \"key\" not found in HTML from server"
