namespace Pandacap.LowLevel

open System
open FSharp.Data

module WeasylScraper =
    type NotificationLink = {
        href: string
        name: string
    }

    let private ExtractNotificationGroups (html: string) = [
        let doc = HtmlDocument.Parse(html)

        let baseUri = new Uri("https://www.weasyl.com/")

        for group in doc.CssSelect("#messages-checkboxes > .group") do {|
            id = group.AttributeValue("id")
            notifications = [
                for item in group.CssSelect(".item") do {|
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
                |}
            ]
        |}
    ]

    type ExtractedNotification = {
        Id: string
        PostUrl: string
        Time: DateTimeOffset
        UserName: string
        UserUrl: string
    }

    let ExtractNotifications html = [
        for group in ExtractNotificationGroups html do
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

    type ExtractedJournal = {
        time: DateTimeOffset
        user: NotificationLink
        post: NotificationLink
    }

    let ExtractJournals html = [
        for group in ExtractNotificationGroups html do
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

    type Note = {
        title: string
        sender: string
        sender_url: string
        time: DateTimeOffset
    }

    let ExtractNotes (html: string) = [
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
