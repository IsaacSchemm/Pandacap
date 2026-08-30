namespace Pandacap.ATProto.Feeds

open FSharp.Control
open Pandacap.ATProto.Feeds.Interfaces
open Pandacap.ATProto.Services.Interfaces
open Pandacap.Text
open Pandacap.UI.Badges
open Pandacap.UI.Elements

type ATProtoFeedProvider(
    blueskyService: IBlueskyService
) =
    let maskedThumbnail = {
        new IPostThumbnail with
            member _.Url = $"/images/tr-gray.svg"
            member _.AltText = ""
    }

    interface IATProtoFeedProvider with
        member _.GetBlueskyPostsAsync(pds, did) = asyncSeq {
            for item in blueskyService.GetNewestPostsAsync(pds, did) do
                let comp = item.Ref.Uri.Components
                let post = item.Value

                yield {
                    new IPost with
                        member _.Badge = Badges.ATProto
                        member _.DisplayTitle = post.Text |> ExcerptGenerator.FromText 60
                        member _.Id = item.Ref.CID
                        member _.InternalUrl = $"/ATProto/ViewBlueskyPost?did={comp.DID}&rkey={comp.RecordKey}"
                        member _.ExternalUrl = $"https://bsky.app/profile/{comp.DID}/post/{comp.RecordKey}"
                        member _.PostedAt = post.CreatedAt
                        member _.ProfileUrl = $"https://bsky.app/profile/{comp.DID}"
                        member _.Thumbnails = [
                            if post.AdultContent then
                                maskedThumbnail
                            else
                                for image in post.Images do {
                                    new IPostThumbnail with
                                        member _.Url = $"/ATProto/GetBlob?did={comp.DID}&cid={image.CID}"
                                        member _.AltText = image.Alt
                                }
                        ]
                        member _.Username = null
                        member _.Usericon = null
                }
        }
