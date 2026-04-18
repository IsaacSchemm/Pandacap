namespace Pandacap.DeviantArt

open System
open FSharp.Control
open DeviantArtFs.ParameterTypes
open Pandacap.DeviantArt.Interfaces
open Pandacap.UI.Badges
open Pandacap.UI.Elements

type internal DeviantArtFeedProvider() =
    let orElse defaultValue initial =
        initial |> Option.defaultValue defaultValue

    interface IDeviantArtFeedProvider with
        member _.GetHomeFeedAsync(token, offset) = asyncSeq {
            let! page =
                DeviantArtFs.Api.Browse.PageHomeAsync token MaximumPagingLimit (PagingOffset offset)
                |> Async.AwaitTask

            for item in page.results |> Option.defaultValue [] do {
                new IPost with
                    member _.Badge = Badges.DeviantArt
                    member _.DisplayTitle = item.title |> orElse $"{item.deviationid}"
                    member _.Id = $"{item.deviationid}"
                    member _.InternalUrl = item.url |> orElse null
                    member _.ExternalUrl = item.url |> orElse null
                    member _.PostedAt = item.published_time |> orElse DateTimeOffset.MinValue
                    member _.ProfileUrl = null
                    member _.Thumbnails = seq {
                        for thumbnail in item.thumbs |> Option.defaultValue [] do {
                            new IPostThumbnail with
                                member _.Url = thumbnail.src
                                member _.AltText = ""
                        }
                    }
                    member _.Username = null
                    member _.Usericon = null
            }
        }
