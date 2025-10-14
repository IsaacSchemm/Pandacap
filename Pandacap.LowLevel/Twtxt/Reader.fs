namespace Pandacap.Twtxt

open System
open System.IO
open System.Text
open System.Text.RegularExpressions
open System.Net
open System.Web

module Reader =
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
            |> Seq.map (fun arr -> {|
                text =
                    arr
                    |> Seq.truncate (arr.Length - 1)
                    |> String.concat " "
                url =
                    arr
                    |> Seq.last
            |})
            |> Seq.toList

        {|
            metadata = {|
                url = get "url"
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

                prev = [
                    for str in get "prev" do
                        match str.Split(' ') with
                        | [| hash; url |] ->
                            {|
                                hash = hash
                                url = url
                            |}
                        | _ -> ()
                ]
            |}

            twts = [
                for l in lines do
                    if not (l.StartsWith('#')) then
                        let m = StatusUpdateExpression.Match(l)
                        if m.Success then {|
                            timestamp = DateTimeOffset.Parse(m.Groups[1].Value)
                            text = m.Groups[4].Value.Replace(char 0x2028, '\n')
                        |}
            ]
        |}

    type internal Entity =
    | Text of string
    | Mention of nick: string * url: string

    module internal MentionExtraction =
        type State =
        | ExtractText
        | ExtractNick
        | ExtractUrl of nick: string

        let parse (text: string) = seq {
            let buffer = new StringBuilder()

            let mutable state = ExtractText
            let mutable remaining = List.ofSeq text

            while remaining <> [] do
                match state, remaining with
                | ExtractText, '@' :: '<' :: tail ->
                    yield Text $"{buffer}"
                    buffer.Clear() |> ignore

                    state <- ExtractNick
                    remaining <- tail
                | ExtractNick, '>' :: tail ->
                    yield Mention ($"{buffer}", $"{buffer}")
                    buffer.Clear() |> ignore

                    state <- ExtractText
                    remaining <- tail
                | ExtractNick, ' ' :: tail ->
                    state <- ExtractUrl $"{buffer}"
                    buffer.Clear() |> ignore

                    remaining <- tail
                | ExtractUrl nick, '>' :: tail ->
                    yield Mention (nick, $"{buffer}")
                    buffer.Clear() |> ignore

                    state <- ExtractText
                    remaining <- tail
                | _, c :: tail ->
                    buffer.Append(c) |> ignore
                    remaining <- tail
                | _, [] -> ()

            if state = ExtractText && buffer.Length > 0 then
                yield Text $"{buffer}"
        }

    let ToPlainText str = String.concat "" [
        for entity in MentionExtraction.parse str do
            match entity with
            | Text text -> text
            | Mention (nick, _) -> $"@{nick}"
    ]

    let ToHTML str = String.concat "" [
        for entity in MentionExtraction.parse str do
            match entity with
            | Text text -> HttpUtility.HtmlEncode(text)
            | Mention (nick, url) -> $"""<a href="{HttpUtility.HtmlAttributeEncode(url)}" target='_blank'>@{HttpUtility.HtmlEncode(nick)}</a>"""
    ]
