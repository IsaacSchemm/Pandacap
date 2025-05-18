namespace Pandacap.Clients.ATProto

open System
open System.Net
open System.Net.Http
open System.Net.Http.Headers
open System.Net.Http.Json
open System.Threading.Tasks

/// Any object that contains access and refresh tokens from Bluesky.
type ITokenPair =
    abstract member AccessToken: string
    abstract member RefreshToken: string

/// The response from Bluesky after creating or refreshing a session.
type Tokens = {
    accessJwt: string
    refreshJwt: string
    handle: string
    did: string
} with
    interface ITokenPair with
        member this.AccessToken = this.accessJwt
        member this.RefreshToken = this.refreshJwt

/// Specifies the PDS to connect to. Used in all requests.
type IHost =
    abstract member PDS: string

module Host =
    let Public = { new IHost with member _.PDS = "public.api.bsky.app" }

/// Any object that can be used to authenticate with Bluesky.
/// Contains the user's DID, the PDS to connect to, and the current access token.
type ICredentials =
    inherit IHost
    abstract member DID: string
    abstract member AccessToken: string

/// An object can implement this interface to enable automatic token refresh.
/// When a request fails, this library will use the refresh token to get a new
/// set of tokens, call UpdateTokensAsync to store them, and then retry.
type IAutomaticRefreshCredentials =
    inherit IHost
    inherit ICredentials
    inherit ITokenPair
    abstract member UpdateTokensAsync: newCredentials: ITokenPair -> Task

/// Abstracts HTTP requests to make them easier to write and retry in the rest of the code.
module Requester =
    type Body =
    | NoBody
    | JsonBody of (string * obj) list
    | RawBody of data: byte[] * contentType: string

    type Request = {
        method: HttpMethod
        uri: Uri
        bearerToken: string option
        body: Body
    }

    let buildQueryString (parameters: (string * string) seq) = String.concat "&" [
        for key, value in parameters do
            $"{Uri.EscapeDataString(key)}={Uri.EscapeDataString(value)}"
    ]

    let build (method: HttpMethod) (host: IHost) (procedureName: string) (parameters: (string * string) seq) = {
        method = method
        uri = new Uri($"https://{Uri.EscapeDataString(host.PDS)}/xrpc/{Uri.EscapeDataString(procedureName)}?{buildQueryString parameters}")
        bearerToken = None
        body = NoBody
    }

    let addJsonBody (body: (string * obj) list) (req: Request) = {
        req with body = JsonBody body
    }

    let addBody (body: byte[]) (contentType: string) (req: Request) = {
        req with body = RawBody (body, contentType)
    }

    let addAccessToken (credentials: ICredentials option) (req: Request) = {
        req with bearerToken = credentials |> Option.map (fun c -> c.AccessToken)
    }

    let addRefreshToken (credentials: ITokenPair) (req: Request) = {
        req with bearerToken = Some credentials.RefreshToken
    }

    let sendAsync (httpClient: HttpClient) (request: Request) = task {
        use req = new HttpRequestMessage(request.method, request.uri)

        match request.bearerToken with
        | Some t ->
            req.Headers.Authorization <- new AuthenticationHeaderValue("Bearer", t)
        | None -> ()

        match request.body with
        | RawBody (data, contentType) ->
            let c = new ByteArrayContent(data)
            c.Headers.ContentType <- new MediaTypeHeaderValue(contentType)
            req.Content <- c
        | JsonBody b ->
            req.Content <- JsonContent.Create(dict b)
        | NoBody -> ()

        return! httpClient.SendAsync(req)
    }

    let refreshAuthAndSendAsync<'T> (httpClient: HttpClient) (auto: IAutomaticRefreshCredentials) (req: Request) = task {
        use! tokenResponse =
            build HttpMethod.Post { new IHost with member _.PDS = req.uri.Host } "com.atproto.server.refreshSession" []
            |> addRefreshToken auto
            |> sendAsync httpClient

        tokenResponse.EnsureSuccessStatusCode() |> ignore

        let! newCredentials = tokenResponse.Content.ReadFromJsonAsync<Tokens>()
        do! auto.UpdateTokensAsync(newCredentials)

        return!
            req
            |> addAccessToken (Some auto)
            |> sendAsync httpClient
    }

/// Handles logging in and refreshing tokens.
module Auth =
    let CreateSessionAsync httpClient hostname identifier password = task {
        let host = { new IHost with member _.PDS = hostname }

        use! resp =
            Requester.build HttpMethod.Post host "com.atproto.server.createSession" []
            |> Requester.addJsonBody [
                "identifier", identifier
                "password", password
            ]
            |> Requester.sendAsync httpClient

        if int resp.StatusCode = 400 then
            let! string = resp.Content.ReadAsStringAsync()
            failwith string

        resp.EnsureSuccessStatusCode() |> ignore

        return! resp.Content.ReadFromJsonAsync<Tokens>()
    }

/// Sends HTTP requests, reads and parses responses, and handles automatic token refresh.
module Reader =
    type Error = {
        error: string
        message: string
    }

    exception ErrorException of Error

    let readAsync<'T> (httpClient: HttpClient) (credentials: ICredentials option) (req: Requester.Request) = task {
        use! initialResp =
            req
            |> Requester.addAccessToken credentials
            |> Requester.sendAsync httpClient

        use! finalResp = task {
            match credentials, initialResp.StatusCode with
            | Some (:? IAutomaticRefreshCredentials as auto), HttpStatusCode.BadRequest ->
                let! err = initialResp.Content.ReadFromJsonAsync<Error>()
                if err.error = "ExpiredToken" then
                    return! Requester.refreshAuthAndSendAsync httpClient auto req
                else
                    return raise (ErrorException err)
            | _ ->
                return initialResp
        }

        finalResp.EnsureSuccessStatusCode() |> ignore

        if typedefof<'T> = typedefof<unit> then
            return () :> obj :?> 'T
        else
            return! finalResp.Content.ReadFromJsonAsync<'T>()
    }

type Page =
| FromStart
| FromCursor of string

/// Lists notifications on the user's Bluesky account.
module Notifications =
    type Author = {
        did: string
        handle: string
        displayName: string option
    }

    type Notification = {
        uri: string
        cid: string
        author: Author
        reason: string
        reasonSubject: string
        isRead: bool
        indexedAt: DateTimeOffset
    }

    type NotificationList = {
        cursor: string option
        notifications: Notification list
    } with
        member this.NextPage =
            this.cursor
            |> Option.map FromCursor
            |> Option.toList

    let ListNotificationsAsync httpClient credentials page = task {
        return!
            Requester.build HttpMethod.Get credentials "app.bsky.notification.listNotifications" [
                match page with
                | FromCursor c -> "cursor", c
                | FromStart -> ()
            ]
            |> Reader.readAsync<NotificationList> httpClient (Some credentials)
    }

/// Handles creating and deleting records in the repo, e.g. Bluesky posts.
module Repo =
    type BlobResponse = {
        blob: obj
    }

    type PostImage = {
        blob: obj
        alt: string
        dimensions: (int * int) option
    }

    type PostExternal = {
        description: string
        blob: obj
        title: string
        uri: string
    }

    let UploadBlobAsync httpClient (credentials: ICredentials) (data: byte[]) (contentType: string) (alt: string) = task {
        let! blobResponse =
            Requester.build HttpMethod.Post credentials "com.atproto.repo.uploadBlob" []
            |> Requester.addBody data contentType
            |> Reader.readAsync<BlobResponse> httpClient (Some credentials)
        return {
            blob = blobResponse.blob
            alt = alt
            dimensions =
                try
                    use ms = new System.IO.MemoryStream(data, writable = false)
                    use bitmap = System.Drawing.Bitmap.FromStream(ms)
                    Some (bitmap.Width, bitmap.Height)
                with _ ->
                    None
        }
    }

    type PostEmbed = Images of PostImage list | External of PostExternal | NoEmbed

    type PandacapId = ForPost of Guid | ForFavorite of string

    type Post = {
        text: string
        createdAt: DateTimeOffset
        embed: PostEmbed
        pandacapIds: PandacapId list
    }

    type NewRecord = {
        uri: string
        cid: string
    } with
        member this.RecordKey =
            this.uri.Split('/')
            |> Seq.last

    type Record = Post of Post | EmptyThreadGate of NewRecord

    let CreateRecordAsync httpClient (credentials: ICredentials) (record: Record) = task {
        return!
            Requester.build HttpMethod.Post credentials "com.atproto.repo.createRecord" []
            |> Requester.addJsonBody [
                "repo", credentials.DID

                match record with
                | EmptyThreadGate record ->
                    "collection", "app.bsky.feed.threadgate"
                    "rkey", record.RecordKey
                    "record", dict [
                        "$type", "app.bsky.feed.threadgate" :> obj
                        "post", record.uri
                        "allow", []
                        "createdAt", DateTimeOffset.UtcNow.ToString("o")
                    ]
                | Post post ->
                    "collection", "app.bsky.feed.post"
                    "record", dict [
                        "$type", "app.bsky.feed.post" :> obj
                        "text", post.text
                        "createdAt", post.createdAt.ToString("o")

                        for pandacapId in post.pandacapIds do
                            match pandacapId with
                            | ForPost id ->
                                "pandacapPost", id
                            | ForFavorite id ->
                                "pandacapFavorite", id

                        match post.embed with
                        | Images images ->
                            "embed", dict [
                                "$type", "app.bsky.embed.images" :> obj
                                "images", [
                                    for i in images do dict [
                                        "image", i.blob
                                        "alt", i.alt
                                        match i.dimensions with
                                        | None -> ()
                                        | Some (width, height) ->
                                            "aspectRatio", dict [
                                                "width", width
                                                "height", height
                                            ]
                                    ]
                                ]
                            ]
                        | External ext ->
                            "embed", dict [
                                "$type", "app.bsky.embed.external" :> obj
                                "external", dict [
                                    "description", ext.description :> obj
                                    "thumb", ext.blob
                                    "title", ext.title
                                    "uri", ext.uri
                                ]
                            ]
                        | NoEmbed -> ()
                    ]
            ]
            |> Reader.readAsync<NewRecord> httpClient (Some credentials)
    }

    let DeleteRecordAsync httpClient (credentials: ICredentials) (rkey: string) = task {
        do!
            Requester.build HttpMethod.Post credentials "com.atproto.repo.deleteRecord" []
            |> Requester.addJsonBody [
                "repo", credentials.DID
                "collection", "app.bsky.feed.post"
                "rkey", rkey
            ]
            |> Reader.readAsync<unit> httpClient (Some credentials)
    }

/// Handles requests within app.bsky.feed.
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
        member this.OtherUrls = Option.toList this.bridgyOriginalUrl

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
            match this.uri.Split('/') with
            | [| "at:"; ""; _; "app.bsky.feed.post"; rkey |] ->
                Uri.UnescapeDataString(rkey)
            | _ ->
                failwith "Cannot extract record key from URI"
        member this.Images =
            this.embed
            |> Option.bind (fun e -> e.images)
            |> Option.defaultValue []
        member this.EmbeddedRecords =
            this.embed
            |> Option.bind (fun e -> e.record)
            |> Option.toList

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

    let GetActorLikesAsync httpClient credentials actor page =
        Requester.build HttpMethod.Get credentials "app.bsky.feed.getActorLikes" [
            "actor", actor

            match page with
            | FromCursor c -> "cursor", c
            | FromStart -> ()
        ]
        |> Reader.readAsync<FeedResponse> httpClient (Some credentials)

    let GetAuthorFeedAsync httpClient actor page =
        Requester.build HttpMethod.Get Host.Public "app.bsky.feed.getAuthorFeed" [
            "actor", actor

            match page with
            | FromCursor c -> "cursor", c
            | FromStart -> ()
        ]
        |> Reader.readAsync<FeedResponse> httpClient None

    let GetTimelineAsync httpClient credentials page =
        Requester.build HttpMethod.Get credentials "app.bsky.feed.getTimeline" [
            match page with
            | FromCursor c -> "cursor", c
            | FromStart -> ()
        ]
        |> Reader.readAsync<FeedResponse> httpClient (Some credentials)

/// Handles requests within app.bsky.graph.
module BlueskyGraph =
    type FollowList = {
        cursor: string option
        follows: BlueskyFeed.Author list
    } with
        member this.NextPage =
            this.cursor
            |> Option.map FromCursor
            |> Option.toList

    let GetFollowsAsync httpClient credentials actor page =
        Requester.build HttpMethod.Get credentials "app.bsky.graph.getFollows" [
            "actor", actor

            match page with
            | FromCursor c -> "cursor", c
            | FromStart -> ()
        ]
        |> Reader.readAsync<FollowList> httpClient (Some credentials)
