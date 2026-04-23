namespace Pandacap.DeviantArt.Feeds

open FSharp.Control
open Pandacap.DeviantArt.Feeds.Interfaces
open Pandacap.DeviantArt.Interfaces
open Pandacap.UI.Badges
open Pandacap.UI.Elements

type internal DeviantArtFeedProvider(
    deviantArtClient: IDeviantArtClient
) =
    interface IDeviantArtFeedProvider with
        member _.GetHomeFeedAsync() = asyncSeq {
            for item in deviantArtClient.GetHomeFeedAsync() do {
                new IPost with
                    member _.Badge = Badges.DeviantArt
                    member _.DisplayTitle = item.Title
                    member _.Id = $"{item.DeviationId}"
                    member _.InternalUrl = item.Url
                    member _.ExternalUrl = item.Url
                    member _.PostedAt = item.PublishedTime.Value
                    member _.ProfileUrl = null
                    member _.Thumbnails = seq {
                        for src in item.Thumbnails do {
                            new IPostThumbnail with
                                member _.Url = src
                                member _.AltText = ""
                        }
                    }
                    member _.Username = null
                    member _.Usericon = null
            }
        }
