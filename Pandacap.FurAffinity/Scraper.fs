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

let getOnlineStats (document: HtmlDocument) =
    let regex = new System.Text.RegularExpressions.Regex("([0-9]+) +guests.+ ([0-9]+) +registered.+ ([0-9]+) other")

    let tryParse (str: string) =
        match System.Int32.TryParse(str) with
        | true, x -> x
        | false, _ -> 0

    let mutable allStats = [
        for div in document.CssSelect(".online-stats") do
            let text = div.InnerText()
            let m = regex.Match(text)
            if m.Success then
                {
                    Guests = tryParse m.Groups[1].Value
                    Registered = tryParse m.Groups[2].Value
                    Other = tryParse m.Groups[3].Value
                }
    ]

    {
        Guests = List.sum [for x in allStats do x.Guests]
        Registered = List.sum [for x in allStats do x.Registered]
        Other = List.sum [for x in allStats do x.Other]
    }

let getPostOptions (selector: string) (document: HtmlDocument) = [
    for select in document.CssSelect(selector) do
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
