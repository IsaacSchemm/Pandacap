namespace Pandacap.LowLevel

open System
open FSharp.Data

module DeviantArtScraper =
    let GetIdAsync(url) = task {
        let! document = HtmlDocument.AsyncLoad(url)

        let appurl =
            match [
                for tag in document.CssSelect("meta") do
                    if tag.AttributeValue("property") = "da:appurl" then
                        tag
            ] with
            | [meta] -> meta.AttributeValue("content")
            | _ -> failwith $"DeviantArt app URL not found in meta tag"

        let segments =
            match Uri.TryCreate(appurl, UriKind.Absolute) with
            | true, appuri when appuri.Scheme = "deviantart" -> List.ofArray appuri.Segments
            | _ -> failwith $"Invalid DeviantArt app URL: {appurl}"

        let guid =
            match segments with
            | "/" :: guidStr :: _ -> Guid.Parse(guidStr)
            | _ -> failwith $"Unrecognized DeviantArt app URL: {appurl}"

        return guid
    }
