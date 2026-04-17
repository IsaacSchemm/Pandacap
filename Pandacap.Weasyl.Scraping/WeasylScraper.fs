namespace Pandacap.Weasyl.Scraping

open System
open System.Text.RegularExpressions
open FSharp.Data
open Pandacap.Weasyl.Scraping.Interfaces
open Pandacap.Weasyl.Scraping.Models

type internal WeasylScraper() =
    let extractFavoriteSubmitids (html: string) = 
        let doc = HtmlDocument.Parse(html)
        
        let attributeValues name nodes =
            nodes
            |> Seq.choose (HtmlNode.tryGetAttribute name)
            |> Seq.map HtmlAttribute.value

        let tryGetNextid (urls: string seq) =
            urls
            |> Seq.collect (fun url ->
                printfn "%s" url
                let o = Regex.Matches(url, "[&\?]nextid=([0-9]+)")
                printfn "%d" o.Count
                o)
            |> Seq.map (fun m -> m.Groups[1].Value)
            |> Seq.map Int32.Parse
            |> Seq.tryHead

        let tryParseInt32 (str: string) =
            match Int32.TryParse(str) with
            | true, value -> Some value
            | false, _ -> None

        let tryGetSubmitid (url: string) =
            match url.Split('/') with
            | [| ""; _; "submissions"; str; _ |] -> tryParseInt32 str
            | _ -> None

        {
            submitids =
                doc.CssSelect("ul.thumbnail-grid > li")
                |> Seq.collect (fun node -> node.CssSelect("a"))
                |> attributeValues "href"
                |> Seq.choose tryGetSubmitid
                |> Seq.distinct
                |> Seq.toList
            nextid =
                doc.CssSelect("a[rel=next]")
                |> attributeValues "href"
                |> tryGetNextid
                |> Option.toNullable
        }

    let extractNotificationGroups (html: string) = [
        let doc = HtmlDocument.Parse(html)

        let baseUri = new Uri("https://www.weasyl.com/")

        for group in doc.CssSelect("#messages-checkboxes > .group") do {
            id = group.AttributeValue("id")
            notifications = [
                for item in group.CssSelect(".item") do {
                    users = List.tryHead [
                        for anchor in item.CssSelect("a.username") do {
                            name = anchor.InnerText()
                            href = (new Uri(baseUri, anchor.AttributeValue("href"))).AbsoluteUri
                        }
                    ]
                    time =
                        item.CssSelect("time")
                        |> Seq.map (fun e -> e.AttributeValue("datetime"))
                        |> Seq.map DateTimeOffset.Parse
                        |> Seq.tryHead
                        |> Option.defaultValue DateTimeOffset.UtcNow
                    posts =
                        item.CssSelect("a")
                        |> Seq.map (fun e -> {
                            href =
                                e.TryGetAttribute("href")
                                |> Option.map (fun a -> a.Value())
                                |> Option.map (fun h -> (new Uri(baseUri, h)).GetLeftPart(UriPartial.Path))
                                |> Option.defaultValue ""
                            name = e.InnerText()
                        })
                        |> Seq.where (fun e ->
                            e.href.Contains("/character/")
                            || e.href.Contains("/journal/")
                            || e.href.Contains("/submission/"))
                        |> Seq.tryHead
                }
            ]
        }
    ]

    let extractNotifications html = [
        for group in extractNotificationGroups html do
            for notification in group.notifications do {
                Id = group.id
                PostUrl =
                    notification.posts
                    |> Option.map (fun p -> p.href)
                    |> Option.toObj
                Time = notification.time
                UserName =
                    notification.users
                    |> Option.map (fun u -> u.name)
                    |> Option.toObj
                UserUrl =
                    notification.users
                    |> Option.map (fun u -> u.href)
                    |> Option.toObj
            }
    ]

    let extractJournals html = [
        for group in extractNotificationGroups html do
            if group.id = "journals" then
                for notification in group.notifications do
                    match notification.posts, notification.users with
                    | Some post, Some user ->
                        {
                            time = notification.time
                            user = user
                            post = post
                        }
                    | _ -> ()
    ]

    let extractNotes (html: string) = [
        let doc = HtmlDocument.Parse(html)

        let baseUri = new Uri("https://www.weasyl.com/")

        for row in doc.CssSelect("table.notes-list tbody tr") do
            let cells = row.Elements()

            yield {
                title =
                    cells[1].InnerText()
                sender =
                    cells[2].InnerText()
                sender_url =
                    cells[2].CssSelect("a")
                    |> Seq.choose (fun e -> e.TryGetAttribute("href"))
                    |> Seq.map (fun a -> a.Value())
                    |> Seq.map (fun str -> new Uri(baseUri, str))
                    |> Seq.map (fun u -> u.AbsoluteUri)
                    |> Seq.tryHead
                    |> Option.toObj
                time =
                    cells[3].CssSelect("time")
                    |> Seq.choose (fun e -> e.TryGetAttribute("datetime"))
                    |> Seq.map (fun e -> e.Value())
                    |> Seq.map DateTimeOffset.Parse
                    |> Seq.tryHead
                    |> Option.defaultValue DateTimeOffset.UtcNow
            }
    ]

    interface IWeasylScraper with
        member this.ExtractFavoriteSubmitids(html) = extractFavoriteSubmitids html
        member this.ExtractJournals(html) = extractJournals html
        member this.ExtractNotes(html) = extractNotes html
        member this.ExtractNotificationGroups(html) = extractNotificationGroups html
        member this.ExtractNotifications(html) = extractNotifications html
