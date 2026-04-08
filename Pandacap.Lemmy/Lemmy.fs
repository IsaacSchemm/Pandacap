namespace Pandacap.Lemmy

open System
open System.Net.Http
open System.Net.Http.Json
open Pandacap.Lemmy.Models

module internal Lemmy =
    let asyncGetCommunity (httpClient: HttpClient) (host: string) (name: string) = async {
        let! cancellationToken = Async.CancellationToken

        use req = new HttpRequestMessage(HttpMethod.Get, $"https://{host}/api/v3/community?name={Uri.EscapeDataString(name)}")
        req.Headers.Accept.ParseAdd("application/json")

        use! resp = httpClient.SendAsync(req, cancellationToken) |> Async.AwaitTask
        resp.EnsureSuccessStatusCode() |> ignore

        let! obj = resp.Content.ReadFromJsonAsync<GetCommunityResponse>(cancellationToken) |> Async.AwaitTask

        return obj
    }

    let asyncGetPost (httpClient: HttpClient) (host: string) (id: int) = async {
        let! cancellationToken = Async.CancellationToken

        use req = new HttpRequestMessage(HttpMethod.Get, $"https://{host}/api/v3/post?id={id}")
        req.Headers.Accept.ParseAdd("application/json")

        use! resp = httpClient.SendAsync(req, cancellationToken) |> Async.AwaitTask
        let! obj = resp.Content.ReadFromJsonAsync<GetPostResponse>(cancellationToken) |> Async.AwaitTask

        return obj
    }

    let asyncGetPosts (httpClient: HttpClient) (host: string) (parameters: GetPostsParameter seq) = async {
        let! cancellationToken = Async.CancellationToken

        let qs = String.concat "&" [
            for parameter in parameters do
                match parameter with
                | GetPostsParameter.Sort x -> $"sort={x}"
                | GetPostsParameter.Page x -> $"page={x}"
                | GetPostsParameter.Limit x -> $"limit={x}"
                | GetPostsParameter.CommunityId x -> $"community_id={x}"
                | GetPostsParameter.CommunityName x -> $"community_name={Uri.EscapeDataString(x)}"
                | GetPostsParameter.PageCursor x -> $"page_cursor={Uri.EscapeDataString(x)}"
        ]

        use req = new HttpRequestMessage(HttpMethod.Get, $"https://{host}/api/v3/post/list?{qs}")
        req.Headers.Accept.ParseAdd("application/json")

        use! resp = httpClient.SendAsync(req, cancellationToken) |> Async.AwaitTask
        let! obj = resp.Content.ReadFromJsonAsync<GetPostsResponse>(cancellationToken) |> Async.AwaitTask

        return obj
    }

    let asyncGetComments (httpClient: HttpClient) (host: string) (parameters: GetCommentsParameter seq) = async {
        let! cancellationToken = Async.CancellationToken

        let qs = String.concat "&" [
            for parameter in parameters do
                match parameter with
                | GetCommentsParameter.Sort x -> $"sort={x}"
                | GetCommentsParameter.Page x -> $"page={x}"
                | GetCommentsParameter.Limit x -> $"limit={x}"
                | GetCommentsParameter.CommunityId x -> $"community_id={x}"
                | GetCommentsParameter.CommunityName x -> $"community_name={Uri.EscapeDataString(x)}"
                | GetCommentsParameter.PostId x -> $"post_id={x}"
                | GetCommentsParameter.ParentId x -> $"parent_id={x}"
        ]

        use req = new HttpRequestMessage(HttpMethod.Get, $"https://{host}/api/v3/comment/list?{qs}")
        req.Headers.Accept.ParseAdd("application/json")

        use! resp = httpClient.SendAsync(req, cancellationToken) |> Async.AwaitTask
        let! obj = resp.Content.ReadFromJsonAsync<GetCommentsResponse>(cancellationToken) |> Async.AwaitTask

        return obj
    }

    let restructure (source: CommentObject seq) = 
        let sorted =
            source
            |> Seq.distinct
            |> Seq.groupBy (fun co -> co.comment.Parent)
            |> dict

        let rec createBranches parent = [
            match sorted.TryGetValue(parent) with
            | false, _ -> ()
            | true, found ->
                for co in found do {
                    root = co
                    replies = createBranches co.comment.id
                }
        ]

        createBranches 0
