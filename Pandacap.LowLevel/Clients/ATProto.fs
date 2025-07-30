namespace Pandacap.Clients.ATProto

open System
open System.Net.Http
open System.Net.Http.Json

module RecordKey =
    let Extract (uri: string) =
        match uri with
        | null -> null
        | _ -> uri.Split('/') |> Array.last |> Uri.UnescapeDataString

module Requests =
    let buildQueryString (parameters: (string * string) seq) = String.concat "&" [
        for (key, value) in parameters do
            $"{Uri.EscapeDataString(key)}={Uri.EscapeDataString(value)}"
    ]

    let getAsync<'T> (httpClient: HttpClient) (pds: string) (procedureName: string) parameters = task {
        use! resp = httpClient.GetAsync($"https://{pds}/xrpc/{Uri.EscapeDataString(procedureName)}?{buildQueryString parameters}")
        return! resp.EnsureSuccessStatusCode().Content.ReadFromJsonAsync<'T>()
    }

type Page =
| FromStart
| FromCursor of string

module Profile =
    type ProfileResponse = {
        did: string
        handle: string
        displayName: string
        avatar: string
        description: string
    }

    let GetProfileAsync httpClient pds actor =
        Requests.getAsync<ProfileResponse> httpClient pds "app.bsky.actor.getProfile" [
            "actor", actor
        ]

module BlueskyFeed =
    type Author = {
        did: string
        handle: string
        displayName: string option
        avatar: string option
    } with
        member this.DisplayNameOrNull = Option.toObj this.displayName
        member this.AvatarOrNull = Option.toObj this.avatar

    type Image = {
        thumb: string
        fullsize: string
        alt: string
    }

    type EmbeddedRecord = {
        cid: string
        uri: string
    }

    type Embed = {
        images: Image list option
        record: EmbeddedRecord option
    }

    type ReplyReference = {
        uri: string
        cid: string
    } with
        member this.UriComponents =
            match this.uri.Split('/') with
            | [| "at:"; ""; did; "app.bsky.feed.post"; rkey |] ->
                {|
                    did = Uri.UnescapeDataString(did)
                    rkey = Uri.UnescapeDataString(rkey)
                |}
            | _ ->
                failwith "Cannot extract record key from URI"

    type Reply = {
        parent: ReplyReference
        root: ReplyReference
    }

    type Record = {
        createdAt: DateTimeOffset
        text: string
        reply: Reply option
        bridgyOriginalUrl: string option
    } with
        member this.InReplyTo = Option.toList this.reply
        member this.ActivityPubUrls = Option.toList this.bridgyOriginalUrl

    type Label = {
        src: string
        ``val``: string
    }

    type Post = {
        uri: string
        cid: string
        author: Author
        record: Record
        embed: Embed option
        indexedAt: DateTimeOffset
        labels: Label list
    } with
        member this.RecordKey =
            RecordKey.Extract this.uri
        member this.Images =
            this.embed
            |> Option.bind (fun e -> e.images)
            |> Option.defaultValue []
        member this.EmbeddedRecords =
            this.embed
            |> Option.bind (fun e -> e.record)
            |> Option.toList

    type PostsResponse = {
        posts: Post list
    }

    let GetPostsAsync httpClient pds uris =
        Requests.getAsync<PostsResponse> httpClient pds "app.bsky.feed.getPosts" [
            for uri in uris do
                "uris", uri
        ]

    type Reason = {
        ``$type``: string
        by: Author
        indexedAt: DateTimeOffset
    }

    type FeedItem = {
        post: Post
        reason: Reason option
    } with
        member this.By =
            match this.reason with
            | Some r when r.``$type`` = "app.bsky.feed.defs#reasonRepost" -> r.by
            | _ -> this.post.author
        member this.IndexedAt =
            match this.reason with
            | Some r when r.``$type`` = "app.bsky.feed.defs#reasonRepost" -> r.indexedAt
            | _ -> this.post.indexedAt

    type FeedResponse = {
        cursor: string option
        feed: FeedItem list
    } with
        member this.NextPage =
            this.cursor
            |> Option.map FromCursor
            |> Option.toList

    let GetActorLikesAsync httpClient pds actor page =
        Requests.getAsync<FeedResponse> httpClient pds "app.bsky.feed.getActorLikes" [
            "actor", actor

            match page with
            | FromCursor c -> "cursor", c
            | FromStart -> ()
        ]

    let GetAuthorFeedAsync httpClient pds actor page =
        Requests.getAsync<FeedResponse> httpClient pds "app.bsky.feed.getAuthorFeed" [
            "actor", actor

            match page with
            | FromCursor c -> "cursor", c
            | FromStart -> ()
        ]
