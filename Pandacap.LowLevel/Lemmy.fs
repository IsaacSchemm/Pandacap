namespace Pandacap.LowLevel

open System
open System.Net.Http
open System.Net.Http.Json
open System.Threading

module Lemmy =
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
        member this.Bodies = Option.toList this.body
        member this.ThumbnailUrls = Option.toList this.thumbnail_url

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

    type PostObject = {
        post: Post
        creator: Creator
        counts: Counts
    }

    type GetPostsResponse = {
        posts: PostObject list
        next_page: string option
    } with
        member this.HasNextPage = Option.isSome this.next_page

    type Sort =
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
    | Sort of Sort
    | Page of int
    | Limit of int
    | CommunityId of int
    | CommunityName of string
    | PageCursor of string

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
