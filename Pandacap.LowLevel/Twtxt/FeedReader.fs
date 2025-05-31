namespace Pandacap.LowLevel.Twtxt

open System
open System.IO
open System.Text
open System.Text.RegularExpressions

module FeedReader =
    let MetadataExpression = new Regex("^# *([^ ]+) *= *(.*)")
    let StatusUpdateExpression = new Regex("^([^\t]+)\t(\(#([^ ]+)\) )?(.*)")

    let ReadFeed (contents: byte[]) =
        use ms = new MemoryStream(contents, writable = false)
        use sr = new StreamReader(ms, Encoding.UTF8)

        let lines = [
            let mutable l = sr.ReadLine()
            while not (isNull l) do
                l
                l <- sr.ReadLine()
        ]

        let pairs = [
            for l in lines do
                let m = MetadataExpression.Match(l)
                if m.Success then
                    m.Groups[1].Value, m.Groups[2].Value
        ]

        let get name =
            pairs
            |> Seq.where (fun (k, _) -> k = name)
            |> Seq.map (fun (_, v) -> v)
            |> Seq.toList

        let getLinks name =
            get name
            |> Seq.map (fun str -> str.Split(' '))
            |> Seq.where (fun arr -> arr.Length > 1)
            |> Seq.map (fun arr -> {
                text =
                    arr
                    |> Seq.truncate (arr.Length - 1)
                    |> String.concat " "
                url =
                    arr
                    |> Seq.last
            })
            |> Seq.toList

        {
            metadata = {
                url = get "url" |> List.map Uri
                nick = get "nick"
                avatar = get "avatar"
                follow = getLinks "follow"
                link = getLinks "link"

                refresh = [
                    for str in get "refresh" do
                        match Int32.TryParse(str) with
                        | true, v -> v
                        | false, _ -> ()
                ]
            }

            twts = [
                for l in lines do
                    if not (l.StartsWith('#')) then
                        let m = StatusUpdateExpression.Match(l)
                        if m.Success then {
                            timestamp = DateTimeOffset.Parse(m.Groups[1].Value)
                            text = m.Groups[4].Value.Replace(char 0x2028, '\n')
                            replyContext =
                                match m.Groups[3].Value with
                                | "" -> NoReplyContext
                                | x -> Hash x
                        }
            ]
        }
