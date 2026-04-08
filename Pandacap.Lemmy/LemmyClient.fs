namespace Pandacap.Lemmy

open System
open System.Net.Http
open System.Net.Http.Json
open FSharp.Control
open Pandacap.Lemmy.Models
open Pandacap.Lemmy.Interfaces

type LemmyClient(
    httpClientFactory: IHttpClientFactory
) =
    interface ILemmyClient with
        member _.GetCommentsAsync(host, post_id, sort) = asyncSeq {
            use client = httpClientFactory.CreateClient()

            let mutable page = 1
            let mutable finished = false

            while not finished do
                let! resp = Lemmy.asyncGetComments client host [
                    Sort sort
                    PostId post_id
                    Page page
                ]

                yield! resp.comments

                if List.isEmpty resp.comments then
                    finished <- true

                page <- page + 1
        }

        member _.GetCommunityAsync(host, name, cancellationToken) = task {
            use client = httpClientFactory.CreateClient()

            let! communityResponse = Async.StartAsTask(
                Lemmy.asyncGetCommunity client host name,
                cancellationToken = cancellationToken)

            return communityResponse.community_view.community
        }

        member _.GetPostAsync(host, id, cancellationToken) = task {
            use client = httpClientFactory.CreateClient()

            let! postResponse = Async.StartAsTask(
                Lemmy.asyncGetPost client host id,
                cancellationToken = cancellationToken)

            return struct (postResponse.post_view, postResponse.community_view.community)
        }

        member _.GetPostsAsync(host, community_id, sort, start_page) = asyncSeq {
            use client = httpClientFactory.CreateClient()

            let mutable page = start_page
            let mutable finished = false

            while not finished do
                let! resp = Lemmy.asyncGetPosts client host [
                    GetPostsParameter.Sort sort
                    GetPostsParameter.Page page
                    GetPostsParameter.Limit 10
                    GetPostsParameter.CommunityId community_id
                ]

                yield! resp.posts

                if List.isEmpty resp.posts then
                    finished <- true

                page <- page + 1
        }

        member _.Restructure(comments) = Lemmy.restructure comments
