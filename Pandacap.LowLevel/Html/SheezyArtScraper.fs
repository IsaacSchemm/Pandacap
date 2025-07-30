namespace Pandacap.Html

open System
open System.Text.RegularExpressions
open FSharp.Data

module SheezyArtScraper =
    let private imagePattern = new Regex("https://cdn.sheezy.art/........................")

    let private tryParseUri str =
        match Uri.TryCreate(str, UriKind.Absolute) with
        | true, u -> Some u
        | false, _ -> None

    let private trySplitString (separator: string) (str: string) =
        match str.Split(separator) with
        |  [| a; b |] -> Some (a, b)
        | _ -> None

    let GetProfileAsync(username: string) = task {
        let url = $"https://sheezy.art/{Uri.EscapeDataString(username)}"
        let uri = new Uri(url)

        let toAbsolute (uriStr: string) =
            (new Uri(uri, uriStr)).AbsoluteUri

        let! document = HtmlDocument.AsyncLoad(url)

        return {|
            socialLinks =
                document.CssSelect("a")
                |> Seq.where (fun a ->
                    a.CssSelect(".social-icons").Length = 1)
                |> Seq.choose (fun a ->
                    a
                    |> HtmlNode.tryGetAttribute "href"
                    |> Option.map HtmlAttribute.value
                    |> Option.bind tryParseUri)
                |> Seq.distinct
                |> Seq.toList
            artwork =
                document.CssSelect("main a")
                |> Seq.choose (fun artworkLink ->
                    let href =
                        artworkLink
                        |> HtmlNode.tryGetAttribute "href"
                        |> Option.map HtmlAttribute.value
                    let titleAndArtist =
                        artworkLink
                        |> HtmlNode.tryGetAttribute "title"
                        |> Option.map HtmlAttribute.value
                        |> Option.bind (trySplitString " by ")
                    let thumbnails = [
                        for span in artworkLink.CssSelect("span") do
                            let matches =
                                span
                                |> HtmlNode.tryGetAttribute "style"
                                |> Option.map HtmlAttribute.value
                                |> Option.defaultValue ""
                                |> imagePattern.Matches
                            for m in matches do
                                m.Value
                    ]
                    match href, titleAndArtist, thumbnails with
                    | Some url, Some (title, artist), [thumbnail; avatar] ->
                        Some {|
                            title = title
                            artist =
                                let mutable a = artist
                                while a.StartsWith('@') do
                                    a <- a.Substring(1)
                                a
                            thumbnail =
                                thumbnail
                                |> toAbsolute
                            avatar =
                                avatar
                                |> toAbsolute
                            url =
                                url
                                |> toAbsolute
                            profileUrl =
                                url.Split('/')
                                |> Seq.takeWhile (fun dir -> dir <> "gallery")
                                |> String.concat "/"
                                |> toAbsolute
                        |}
                    | _ -> None)
                |> Seq.where (fun a -> a.artist <> username)
                |> Seq.distinct
                |> Seq.toList
        |}
    }
