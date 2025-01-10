namespace Pandacap.Data

open System
open System.ComponentModel.DataAnnotations.Schema
open FSharp.Data
open Pandacap.PlatformBadges

/// A link attached to a feed item.
type RssFeedEnclosure() =
    member val Url = "" with get, set
    member val MediaType = "" with get, set

/// A post from an Atom or RSS feed followed by the instance owner.
type RssFeedItem() =
    member val Id = Guid.Empty with get, set
    member val FeedTitle = nullString with get, set
    member val FeedWebsiteUrl = nullString with get, set
    member val FeedIconUrl = nullString with get, set
    member val Title = nullString with get, set
    member val Url = nullString with get, set
    member val HtmlDescription = nullString with get, set
    member val Timestamp = DateTimeOffset.MinValue with get, set
    member val Enclosures = new ResizeArray<RssFeedEnclosure>() with get, set

    [<NotMapped>]
    member this.AudioFiles = seq {
        for file in this.Enclosures do
            if file.MediaType = "audio/mpeg" then
                file
    }

    interface IPost with
        member this.Badges = [
            match Uri.TryCreate(this.Url, UriKind.Absolute) with
            | true, uri -> PostPlatform.GetBadge RSS_Atom |> Badge.WithParenthetical uri.Host
            | false, _ -> PostPlatform.GetBadge RSS_Atom
        ]
        member this.DisplayTitle = this.Title |> orString $"{this.Id}"
        member this.Id = $"{this.Id}"
        member _.IsDismissable = true
        member this.LinkUrl = this.Url
        member this.ProfileUrl = this.FeedWebsiteUrl
        member this.Thumbnails =
            try
                let html = HtmlDocument.Parse(this.HtmlDescription |> orString "") in seq {
                    for node in html.Descendants("img") do
                        match node.TryGetAttribute("src") with
                        | None -> ()
                        | Some srcAttr -> {
                            new IPostThumbnail with
                                member _.AltText =
                                    node.TryGetAttribute("alt")
                                    |> Option.map (fun attr -> attr.Value())
                                    |> Option.defaultValue ""
                                member _.Url = srcAttr.Value()
                        }
                }
            with
            | _ -> Seq.empty
        member this.Timestamp = this.Timestamp
        member this.Usericon = this.FeedIconUrl
        member this.Username = this.FeedWebsiteUrl
