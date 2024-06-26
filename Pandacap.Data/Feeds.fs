﻿namespace Pandacap.Data

open System
open FSharp.Data

/// An Atom or RSS feed followed by the instance owner.
type Feed() =
    member val Id = Guid.Empty with get, set
    member val FeedUrl = "" with get, set
    member val FeedTitle = nullString with get, set
    member val FeedWebsiteUrl = nullString with get, set
    member val FeedIconUrl = nullString with get, set
    member val LastCheckedAt = DateTimeOffset.MinValue with get, set

/// A post from an Atom or RSS feed followed by the instance owner.
type FeedItem() =
    member val Id = Guid.Empty with get, set
    member val FeedTitle = nullString with get, set
    member val FeedWebsiteUrl = nullString with get, set
    member val FeedIconUrl = nullString with get, set
    member val Title = nullString with get, set
    member val Url = nullString with get, set
    member val HtmlDescription = nullString with get, set
    member val Timestamp = DateTimeOffset.MinValue with get, set

    interface IPost with
        member this.DisplayTitle = this.Title |> orString $"{this.Id}"
        member this.Id = $"{this.Id}"
        member this.Timestamp = this.Timestamp
        member this.ThumbnailUrls =
            let html = HtmlDocument.Parse (this.HtmlDescription |> orString "")

            html.Descendants "img"
            |> Seq.choose (fun node -> node.TryGetAttribute "src")
            |> Seq.map (fun attr -> attr.Value())
        member this.LinkUrl = this.Url
        member this.Usericon = this.FeedIconUrl
        member this.Username = this.FeedWebsiteUrl
