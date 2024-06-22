namespace Pandacap.Data

open System
open FSharp.Data

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
        member this.Images =
            let html = HtmlDocument.Parse (this.HtmlDescription |> orString "")

            html.Descendants "img"
            |> Seq.choose (fun node -> node.TryGetAttribute "src")
            |> Seq.map (fun attr -> attr.Value())
            |> Seq.map (fun src -> {
                new IPostImage with
                    member _.AltText = null
                    member _.ThumbnailUrl = src
            })
        member this.LinkUrl = this.Url
        member this.Timestamp = this.Timestamp
        member this.Usericon = this.FeedIconUrl
        member this.Username = this.FeedTitle
