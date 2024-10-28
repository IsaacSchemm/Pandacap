namespace Pandacap.LowLevel

open System
open DeviantArtFs.ParameterTypes
open Pandacap.Data
open Pandacap.Types

module DeviantArtFeedPages =
    let GetHomeAsync credentials offset limit = task {
        let def = Option.defaultValue

        let! page = DeviantArtFs.Api.Browse.PageHomeAsync credentials (PagingLimit limit) (PagingOffset offset)

        return {
            Current = [
                for item in page.results |> def [] do {
                    new IPost with
                        member _.Badges = [PostPlatform.GetBadge DeviantArt]
                        member _.DisplayTitle = item.title |> def $"{item.deviationid}"
                        member _.Id = $"{item.deviationid}"
                        member _.IsDismissable = false
                        member _.LinkUrl = item.url |> def null
                        member _.ProfileUrl = null
                        member _.Thumbnails =
                            item.thumbs
                            |> def []
                            |> Seq.sortByDescending (fun t -> t.width * t.height)
                            |> Seq.truncate 1
                            |> Seq.map (fun t -> {
                                new IPostThumbnail with
                                    member _.AltText = ""
                                    member _.Url = t.src
                            })
                        member _.Timestamp = item.published_time |> def DateTimeOffset.UtcNow
                        member _.Usericon = null
                        member _.Username = null
                }
            ]
            Next =
                page.next_offset |> Option.map string
        }
    }
