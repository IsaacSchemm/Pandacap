namespace Pandacap.Data

open System

module TitleGenerator =
    let FromBody(text) =
        let split (sep: char) (str: string) =
            str.Split(sep, StringSplitOptions.TrimEntries)

        let trim (str: string) =
            str.Trim()

        let isNotHashtag (str: string) =
            not (str.StartsWith('#'))

        text
        |> split '\n'
        |> Seq.map (fun line ->
            line
            |> split ' '
            |> Seq.where isNotHashtag
            |> String.concat " ")
        |> String.concat "\n"
        |> trim
