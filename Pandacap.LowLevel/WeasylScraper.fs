namespace Pandacap.LowLevel

open System
open FSharp.Data

module WeasylScraper =
    type Note = {
        title: string
        sender: string
        time: DateTimeOffset
    }

    let ExtractNotes (html: string) = [
        let doc = HtmlDocument.Parse(html)

        for row in doc.CssSelect("table.notes-list tbody tr") do
            let cells = row.Elements()

            yield {
                title =
                    cells[1].InnerText()
                sender =
                    cells[2].InnerText()
                time =
                    cells[3].CssSelect("time")
                    |> Seq.choose (fun e -> e.TryGetAttribute("datetime"))
                    |> Seq.map (fun e -> e.Value())
                    |> Seq.map DateTimeOffset.Parse
                    |> Seq.map (fun d -> d.AddYears(3))
                    |> Seq.tryHead
                    |> Option.defaultValue DateTimeOffset.UtcNow
            }
    ]
