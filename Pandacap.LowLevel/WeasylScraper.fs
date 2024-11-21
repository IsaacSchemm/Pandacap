namespace Pandacap.LowLevel

open System
open FSharp.Data

module WeasylScraper =
    type Notifications = {
        newest_time: DateTimeOffset
    }

    let ExtractNotifications (html: string) =
        let doc = HtmlDocument.Parse(html)

        {
            newest_time =
                doc.CssSelect("time")
                |> Seq.choose (fun e -> e.TryGetAttribute("datetime"))
                |> Seq.map (fun a -> a.Value())
                |> Seq.map DateTimeOffset.Parse
                |> Seq.append [DateTimeOffset.MinValue]
                |> Seq.max
        }

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
