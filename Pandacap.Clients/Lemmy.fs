namespace Pandacap.Clients

open System
open System.Net.Http
open System.Net.Http.Json
open System.Threading
open CommonMark
open Ganss.Xss

module Lemmy =
    let sanitizer = new HtmlSanitizer()

    type Community = {
        id: int
        name: string
        title: string
        nsfw: bool
        actor_id: string
        icon: string option
        banner: string option
    } with
        member this.Icons = Option.toList this.icon
        member this.Banners = Option.toList this.banner

    type CommunityView = {
        community: Community
    }

    type GetCommunityResponse = {
        community_view: CommunityView
    }

    let GetCommunityAsync (httpClient: HttpClient) (host: string) (name: string) (cancellationToken: CancellationToken) = task {
        use req = new HttpRequestMessage(HttpMethod.Get, $"https://{host}/api/v3/community?name={Uri.EscapeDataString(name)}")
        req.Headers.Accept.ParseAdd("application/json")

        use! resp = httpClient.SendAsync(req, cancellationToken)
        resp.EnsureSuccessStatusCode() |> ignore

        let! obj = resp.Content.ReadFromJsonAsync<GetCommunityResponse>(cancellationToken)

        return obj
    }

    type Post = {
        id: int
        name: string
        url: string option
        body: string option
        published: DateTimeOffset
        nsfw: bool
        thumbnail_url: string option
        ap_id: string
        featured_community: bool
    } with
        member this.Urls = Option.toList this.url
        member this.ThumbnailUrls = Option.toList this.thumbnail_url
        member this.Html =
            this.body
            |> Option.map CommonMarkConverter.Convert
            |> Option.map sanitizer.Sanitize
            |> Option.defaultValue ""

    type Creator = {
         name: string
         display_name: string option
         avatar: string option
         actor_id: string
    } with
        member this.Avatars = Option.toList this.avatar
        member this.Names = Option.toList this.display_name @ [this.name]

    type Counts = {
        comments: int
        score: int
    }

    type PostView = {
        post: Post
        creator: Creator
        counts: Counts
    }

    type GetPostResponse = {
        post_view: PostView
        community_view: CommunityView
    }

    type GetPostsResponse = {
        posts: PostView list
        next_page: string option
    } with
        member this.HasNextPage = Option.isSome this.next_page

    type GetPostsSort =
    | Active
    | Hot
    | New
    | Old
    | TopDay
    | TopWeek
    | TopMonth
    | TopYear
    | TopAll
    | MostComments
    | NewComments
    | TopHour
    | TopSixHour
    | TopTwelveHour
    | TopThreeMonth
    | TopSixMonths
    | TopNineMonths
    | Controversial
    | Scaled

    type GetPostsParameter =
    | Sort of GetPostsSort
    | Page of int
    | Limit of int
    | CommunityId of int
    | CommunityName of string
    | PageCursor of string

    let GetPostAsync (httpClient: HttpClient) (host: string) (id: int) (cancellationToken: CancellationToken) = task {
        use req = new HttpRequestMessage(HttpMethod.Get, $"https://{host}/api/v3/post?id={id}")
        req.Headers.Accept.ParseAdd("application/json")

        use! resp = httpClient.SendAsync(req, cancellationToken)
        let! obj = resp.Content.ReadFromJsonAsync<GetPostResponse>(cancellationToken)

        return obj
    }

    let GetPostsAsync (httpClient: HttpClient) (host: string) (parameters: GetPostsParameter seq) (cancellationToken: CancellationToken) = task {
        let qs = String.concat "&" [
            for parameter in parameters do
                match parameter with
                | Sort x -> $"sort={x}"
                | Page x -> $"page={x}"
                | Limit x -> $"limit={x}"
                | CommunityId x -> $"community_id={x}"
                | CommunityName x -> $"community_name={Uri.EscapeDataString(x)}"
                | PageCursor x -> $"page_cursor={Uri.EscapeDataString(x)}"
        ]

        use req = new HttpRequestMessage(HttpMethod.Get, $"https://{host}/api/v3/post/list?{qs}")
        req.Headers.Accept.ParseAdd("application/json")

        use! resp = httpClient.SendAsync(req, cancellationToken)
        let! obj = resp.Content.ReadFromJsonAsync<GetPostsResponse>(cancellationToken)

        return obj
    }

    type Comment = {
        id: int
        postId: int
        content: string
        removed: bool
        published: DateTimeOffset
        ap_id: string
        path: string
    } with
        member this.Parent =
            this.path.Split('.')
            |> Seq.rev
            |> Seq.map Int32.Parse
            |> Seq.skipWhile (fun id -> id <> this.id)
            |> Seq.skipWhile (fun id -> id = this.id)
            |> Seq.head
        member this.Html =
            this.content
            |> CommonMarkConverter.Convert
            |> sanitizer.Sanitize

    type CommentObject = {
        comment: Comment
        creator: Creator
        counts: Counts
    }

    type GetCommentsResponse = {
        comments: CommentObject list
    }

    type GetCommentsSort =
    | Hot
    | Top
    | New
    | Old
    | Controversial

    type GetCommentsParameter =
    | Sort of GetCommentsSort
    | Page of int
    | Limit of int
    | CommunityId of int
    | CommunityName of string
    | PostId of int
    | ParentId of int

    let GetCommentsAsync (httpClient: HttpClient) (host: string) (parameters: GetCommentsParameter seq) (cancellationToken: CancellationToken) = task {
        let qs = String.concat "&" [
            for parameter in parameters do
                match parameter with
                | Sort x -> $"sort={x}"
                | Page x -> $"page={x}"
                | Limit x -> $"limit={x}"
                | CommunityId x -> $"community_id={x}"
                | CommunityName x -> $"community_name={Uri.EscapeDataString(x)}"
                | PostId x -> $"post_id={x}"
                | ParentId x -> $"parent_id={x}"
        ]

        use req = new HttpRequestMessage(HttpMethod.Get, $"https://{host}/api/v3/comment/list?{qs}")
        req.Headers.Accept.ParseAdd("application/json")

        use! resp = httpClient.SendAsync(req, cancellationToken)
        let! obj = resp.Content.ReadFromJsonAsync<GetCommentsResponse>(cancellationToken)

        return obj
    }

    type CommentBranch = {
        root: CommentObject
        replies: CommentBranch list
    }

    let Restructure (source: CommentObject seq) = 
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
