namespace Pandacap.Lemmy.Models

open System
open CommonMark
open Ganss.Xss

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
        |> Option.map (new HtmlSanitizer()).Sanitize
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
        |> (new HtmlSanitizer()).Sanitize

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

type CommentBranch = {
    root: CommentObject
    replies: CommentBranch list
}
