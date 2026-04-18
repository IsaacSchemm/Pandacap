namespace Pandacap.DeviantArt

open FSharp.Control
open Pandacap.DeviantArt.Interfaces
open DeviantArtFs.ParameterTypes
open Pandacap.UI.Elements
open Pandacap.UI.Badges

type internal DeviantArtFeedProvider() =
    interface IDeviantArtFeedProvider with
        member _.GetHomeFeedAsync(token, offset) = asyncSeq {
            for item in DeviantArtFs.Api.Browse.GetHomeAsync token MaximumPagingLimit (PagingOffset offset) do
                match item.title, item.published_time, item.url, item.author with
                | Some title, Some time, Some url, Some author ->
                    yield {
                        new IPost with
                            member _.Badge = Badges.DeviantArt
                            member _.DisplayTitle = title
                            member _.Id = $"{item.deviationid}"
                            member _.InternalUrl = url
                            member _.ExternalUrl = url
                            member _.PostedAt = time
                            member _.ProfileUrl = null
                            member _.Thumbnails = seq {
                                for thumbnail in item.thumbs |> Option.defaultValue [] do {
                                    new IPostThumbnail with
                                        member _.Url = thumbnail.src
                                        member _.AltText = ""
                                }
                            }
                            member _.Username = author.username
                            member _.Usericon = author.usericon
                    }
                | _ -> ()
        }
