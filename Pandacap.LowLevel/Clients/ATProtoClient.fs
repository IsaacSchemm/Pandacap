namespace Pandacap.Clients

open System
open System.Net.Http
open System.Net.Http.Headers
open System.Net.Http.Json
open System.Text.Json.Serialization
open System.Threading.Tasks
open FSharp.Control

module ATProtoClient =
    type DIDDocumentService = {
        id: string
        ``type``: string
        serviceEndpoint: string
    }

    type DIDDocument = {
        id: string
        alsoKnownAs: string list
        service: DIDDocumentService list
    } with
        [<JsonIgnore>]
        member this.Handle =
            this.alsoKnownAs
            |> Seq.where (fun str -> str.StartsWith("at://"))
            |> Seq.map (fun str -> str.Substring(5))
            |> Seq.head
        [<JsonIgnore>]
        member this.PDS =
            this.service
            |> Seq.where (fun service -> service.``type`` = "AtprotoPersonalDataServer")
            |> Seq.choose (fun service ->
                match Uri.TryCreate(service.serviceEndpoint, UriKind.Absolute) with
                | true, u -> Some u.Host
                | false, _ -> None)
            |> Seq.head

    module PLCDirectory =
        let ResolveAsync (httpClient: HttpClient) (did: string) = task {
            use! resp = httpClient.GetAsync($"https://plc.directory/{Uri.EscapeDataString(did)}")
            return! resp.EnsureSuccessStatusCode().Content.ReadFromJsonAsync<DIDDocument>()
        }

    module NSIDs =
        module Bluesky =
            module Actor =
                let Profile = "app.bsky.actor.profile"
            module Feed =
                let Post = "app.bsky.feed.post"

        module WhiteWind =
            module Blog =
                let Entry = "com.whtwnd.blog.entry"

    type Page = FromStart | FromCursor of string

    type Tokens = {
        accessJwt: string
        refreshJwt: string
        handle: string
        did: string
    }

    type MinimalRecord = {
        cid: string
        uri: string
    } with
        [<JsonIgnore>]
        member this.DID =
            match this.uri.Split('/') with
            | [| "at:"; ""; did; _; _ |] -> Uri.UnescapeDataString(did)
            | _ -> failwithf "Cannot extract DID from URI: %s" this.uri
        [<JsonIgnore>]
        member this.RecordKey =
            match this.uri.Split('/') with
            | [| "at:"; ""; _; _; rkey |] -> Uri.UnescapeDataString(rkey)
            | _ -> failwithf "Cannot extract record key from URI: %s" this.uri

    type IHost =
        abstract member PDS: string

    type IToken =
        inherit IHost
        abstract member Token: string

    type ICredentials =
        inherit IHost
        abstract member DID: string
        abstract member AccessToken: string

    type IRefreshCredentials =
        inherit ICredentials
        abstract member RefreshToken: string
        abstract member UpdateTokensAsync: newCredentials: Tokens -> Task

    module Host =
        let Unauthenticated host = {
            new IHost with
                member _.PDS = host
        }

        module Bluesky =
            let PublicAppView = {
                new IHost with
                    member _.PDS = "public.api.bsky.app"
            }

    type internal Body =
    | NoBody
    | JsonBody of (string * obj) list
    | RawBody of data: byte[] * contentType: string

    type internal Request = {
        method: HttpMethod
        procedureName: string
        parameters: (string * string) list
        credentials: IHost
        body: Body
    }

    type XrpcError = {
        error: string
        message: string
    }

    module internal Requests =
        exception XrpcException of XrpcError

        let sendAsync (httpClient: HttpClient) (request: Request) = task {
            let queryString = String.concat "&" [
                for key, value in request.parameters do
                    $"{Uri.EscapeDataString(key)}={Uri.EscapeDataString(value)}"
            ]

            use req = new HttpRequestMessage(
                request.method,
                $"https://{request.credentials.PDS}/xrpc/{Uri.EscapeDataString(request.procedureName)}?{queryString}")

            match request.credentials with
            | :? ICredentials as c ->
                req.Headers.Authorization <- new AuthenticationHeaderValue("Bearer", c.AccessToken)
            | :? IToken as t ->
                req.Headers.Authorization <- new AuthenticationHeaderValue("Bearer", t.Token)
            | _ -> ()

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

        let rec performRequestAsync (httpClient: HttpClient) (req: Request): Task<HttpResponseMessage> = task {
            let! resp = sendAsync httpClient req

            let! error = task {
                if resp.IsSuccessStatusCode then
                    return None
                else
                    let! err = resp.Content.ReadFromJsonAsync<XrpcError>()
                    return Some err
            }

            match error, req.credentials with
            | Some err, (:? IRefreshCredentials as credentials) when err.error = "ExpiredToken" ->
                use! tokenResponse = sendAsync httpClient {
                    method = HttpMethod.Post
                    procedureName = "com.atproto.server.refreshSession"
                    parameters = []
                    credentials = {
                        new IToken with
                            member _.PDS = credentials.PDS
                            member _.Token = credentials.RefreshToken
                    }
                    body = NoBody
                }

                tokenResponse.EnsureSuccessStatusCode() |> ignore

                let! tokens = tokenResponse.Content.ReadFromJsonAsync<Tokens>()
                do! credentials.UpdateTokensAsync(tokens)

                return! performRequestAsync httpClient {
                    req with
                        credentials = {
                            new IToken with
                                member _.PDS = credentials.PDS
                                member _.Token = tokens.accessJwt
                        }
                }
            | Some err, _ ->
                return raise (XrpcException err)
            | None, _ ->
                return resp
        }

        let thenIgnoreAsync (t: Task<HttpResponseMessage>) = task {
            use! response = t
            ignore response
        }

        let thenReadAsync<'T> (t: Task<HttpResponseMessage>) = task {
            use! response = t
            return! response.Content.ReadFromJsonAsync<'T>()
        }

        let thenReadAsAsync<'T> (_: 'T) (t: Task<HttpResponseMessage>) = task {
            use! response = t
            return! response.Content.ReadFromJsonAsync<'T>()
        }

    module Identity =
        let ResolveHandleAsync httpClient credentials handle =
            Requests.sendAsync httpClient {
                method = HttpMethod.Get
                procedureName = "com.atproto.identity.resolveHandle"
                parameters = [
                    "handle", handle
                ]
                credentials = credentials
                body = NoBody
            }
            |> Requests.thenReadAsAsync {| did = "" |}

    module Server =
        let CreateSessionAsync httpClient hostname identifier password = task {
            use! resp = Requests.sendAsync httpClient {
                method = HttpMethod.Post
                procedureName = "com.atproto.server.createSession"
                parameters = []
                credentials = Host.Unauthenticated hostname
                body = JsonBody [
                    "identifier", identifier
                    "password", password
                ]
            }

            if int resp.StatusCode = 400 then
                let! string = resp.Content.ReadAsStringAsync()
                failwith string

            resp.EnsureSuccessStatusCode() |> ignore

            return! resp.Content.ReadFromJsonAsync<Tokens>()
        }

    let extractRecordKey (uri: string) =
        match uri with
        | null -> null
        | _ -> uri.Split('/') |> Array.last |> Uri.UnescapeDataString

    module Bluesky =
        type Profile = {
            did: string
            handle: string
            displayName: string option
            avatar: string option
            description: string option
        } with
            [<JsonIgnore>]
            member this.DisplayName = Option.toObj this.displayName
            [<JsonIgnore>]
            member this.Avatar = Option.toObj this.avatar
            [<JsonIgnore>]
            member this.Description = Option.toObj this.description

        module Actor =
            let GetProfileAsync httpClient credentials actor =
                {
                    method = HttpMethod.Get
                    procedureName = "app.bsky.actor.getProfile"
                    parameters = [
                        "actor", actor
                    ]
                    credentials = credentials
                    body = NoBody
                }
                |> Requests.performRequestAsync httpClient
                |> Requests.thenReadAsync<Profile>

        module Notification =
            type Notification = {
                uri: string
                cid: string
                author: Profile
                reason: string
                reasonSubject: string
                isRead: bool
                indexedAt: DateTimeOffset
            } with
                [<JsonIgnore>]
                member this.RecordKey = extractRecordKey this.uri
                [<JsonIgnore>]
                member this.ReasonSubject = {|
                    RecordKey = extractRecordKey this.reasonSubject
                |}

            type NotificationList = {
                cursor: string option
                notifications: Notification list
            } with
                [<JsonIgnore>]
                member this.NextPage =
                    this.cursor
                    |> Option.map FromCursor
                    |> Option.toList

            let ListNotificationsAsync httpClient credentials page =
                {
                    method = HttpMethod.Get
                    procedureName = "app.bsky.notification.listNotifications"
                    parameters = [
                        match page with
                        | FromCursor c -> "cursor", c
                        | FromStart -> ()
                    ]
                    credentials = credentials
                    body = NoBody
                }
                |> Requests.performRequestAsync httpClient
                |> Requests.thenReadAsync<NotificationList>

        module Feed =
            type Image = {
                thumb: string
                fullsize: string
                alt: string
            }

            type Embed = {
                images: Image list option
                record: MinimalRecord option
            }

            type Reply = {
                parent: MinimalRecord
                root: MinimalRecord
            }

            type Record = {
                createdAt: DateTimeOffset
                embed: Embed option
                text: string
                reply: Reply option
                bridgyOriginalUrl: string option
            } with
                [<JsonIgnore>]
                member this.InReplyTo = Option.toObj this.reply
                [<JsonIgnore>]
                member this.ActivityPubUrl = Option.toObj this.bridgyOriginalUrl

            type Label = {
                src: string
                ``val``: string
            }

            type Post = {
                uri: string
                cid: string
                author: Profile
                record: Record
                embed: Embed option
                indexedAt: DateTimeOffset
                labels: Label list
            } with
                [<JsonIgnore>]
                member this.RecordKey =
                    extractRecordKey this.uri
                [<JsonIgnore>]
                member this.Images =
                    this.embed
                    |> Option.bind (fun e -> e.images)
                    |> Option.defaultValue []
                [<JsonIgnore>]
                member this.EmbeddedRecord =
                    this.embed
                    |> Option.bind (fun e -> e.record)
                    |> Option.toObj

            type PostsResponse = {
                posts: Post list
            }

            let GetPostsAsync httpClient credentials uris =
                {
                    method = HttpMethod.Get
                    procedureName = "app.bsky.feed.getPosts"
                    parameters = [
                        for uri in uris do
                            "uris", uri
                    ]
                    credentials = credentials
                    body = NoBody
                }
                |> Requests.performRequestAsync httpClient
                |> Requests.thenReadAsync<PostsResponse>

            type PostThread = {
                post: Post
                replies: PostThread list option
            } with
                [<JsonIgnore>]
                member this.Replies =
                    this.replies
                    |> Option.defaultValue []

            type PostThreadResponse = {
                thread: PostThread
            }

            let GetPostThreadAsync httpClient credentials uri =
                {
                    method = HttpMethod.Get
                    procedureName = "app.bsky.feed.getPostThread"
                    parameters = [
                        "uri", uri
                        "depth", "2"
                    ]
                    credentials = credentials
                    body = NoBody
                }
                |> Requests.performRequestAsync httpClient
                |> Requests.thenReadAsync<PostThreadResponse>

            type Reason = {
                ``$type``: string
                by: Profile
                indexedAt: DateTimeOffset
            }

            type FeedItem = {
                post: Post
                reason: Reason option
            } with
                [<JsonIgnore>]
                member this.By =
                    match this.reason with
                    | Some r when r.``$type`` = "app.bsky.feed.defs#reasonRepost" -> r.by
                    | _ -> this.post.author
                [<JsonIgnore>]
                member this.IndexedAt =
                    match this.reason with
                    | Some r when r.``$type`` = "app.bsky.feed.defs#reasonRepost" -> r.indexedAt
                    | _ -> this.post.indexedAt

            type FeedResponse = {
                cursor: string option
                feed: FeedItem list
            } with
                [<JsonIgnore>]
                member this.NextPage =
                    this.cursor
                    |> Option.map FromCursor
                    |> Option.toList

            let GetActorLikesAsync httpClient credentials actor page =
                {
                    method = HttpMethod.Get
                    procedureName = "app.bsky.feed.getActorLikes"
                    parameters = [
                        "actor", actor

                        match page with
                        | FromCursor c -> "cursor", c
                        | FromStart -> ()
                    ]
                    credentials = credentials
                    body = NoBody
                }
                |> Requests.performRequestAsync httpClient
                |> Requests.thenReadAsync<FeedResponse>

            let GetAuthorFeedAsync httpClient credentials actor page =
                {
                    method = HttpMethod.Get
                    procedureName = "app.bsky.feed.getAuthorFeed"
                    parameters = [
                        "actor", actor

                        match page with
                        | FromCursor c -> "cursor", c
                        | FromStart -> ()
                    ]
                    credentials = credentials
                    body = NoBody
                }
                |> Requests.performRequestAsync httpClient
                |> Requests.thenReadAsync<FeedResponse>

    /// Handles creating and deleting records in the repo, e.g. Bluesky posts.
    module Repo =
        type UploadedBlob = {
            Blob: obj
            AltText: string
            Dimensions: (int * int) option
        }

        let UploadBlobAsync httpClient credentials (data: byte[]) (contentType: string) (alt: string) = task {
            let! blobResponse =
                {
                    method = HttpMethod.Post
                    procedureName = "com.atproto.repo.uploadBlob"
                    parameters = []
                    credentials = credentials
                    body = RawBody (data, contentType)
                }
                |> Requests.performRequestAsync httpClient
                |> Requests.thenReadAsAsync {|
                    blob = null :> obj
                |}

            return {
                Blob = blobResponse.blob
                AltText = alt
                Dimensions =
                    try
                        use ms = new System.IO.MemoryStream(data, writable = false)
                        use bitmap = System.Drawing.Bitmap.FromStream(ms)
                        Some (bitmap.Width, bitmap.Height)
                    with _ ->
                        None
            }
        }

        type ExternalEmbed = {
            Description: string
            Blob: obj
            Title: string
            Uri: string
        }

        type PostEmbed = Images of UploadedBlob list | External of ExternalEmbed | NoEmbed

        type PandacapMetadata = PostId of Guid

        type InReplyTo = {
            root: MinimalRecord
            parent: MinimalRecord
        }

        type Post = {
            Text: string
            CreatedAt: DateTimeOffset
            Embed: PostEmbed
            InReplyTo: InReplyTo list
            PandacapMetadata: PandacapMetadata list
        }

        type ThreadGate = {
            Uri: string
        }

        type RecordToCreate =
        | Post of Post
        | EmptyThreadGate of ThreadGate
        | Like of MinimalRecord

        let CreateRecordAsync httpClient (credentials: ICredentials) (record: RecordToCreate) = task {
            return!
                {
                    method = HttpMethod.Post
                    procedureName = "com.atproto.repo.createRecord"
                    parameters = []
                    credentials = credentials
                    body = JsonBody [
                        "repo", credentials.DID

                        match record with
                        | EmptyThreadGate x ->
                            "collection", "app.bsky.feed.threadgate"
                            "rkey", extractRecordKey x.Uri
                            "record", dict [
                                "$type", "app.bsky.feed.threadgate" :> obj
                                "post", x.Uri
                                "allow", []
                                "createdAt", DateTimeOffset.UtcNow.ToString("o")
                            ]

                        | Post post ->
                            "collection", "app.bsky.feed.post"
                            "record", dict [
                                "$type", "app.bsky.feed.post" :> obj
                                "text", post.Text
                                "createdAt", post.CreatedAt.ToString("o")

                                for r in post.InReplyTo do
                                    "reply", dict [
                                        "root", r.root
                                        "parent", r.parent
                                    ]

                                for pm in post.PandacapMetadata do
                                    match pm with
                                    | PostId id ->
                                        "pandacapPost", id

                                match post.Embed with
                                | Images images ->
                                    "embed", dict [
                                        "$type", "app.bsky.embed.images" :> obj
                                        "images", [
                                            for i in images do dict [
                                                "image", i.Blob
                                                "alt", i.AltText
                                                match i.Dimensions with
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
                                            "description", ext.Description :> obj
                                            "thumb", ext.Blob
                                            "title", ext.Title
                                            "uri", ext.Uri
                                        ]
                                    ]
                                | NoEmbed -> ()
                            ]

                        | Like subject ->
                            "collection", "app.bsky.feed.like"
                            "record", dict [
                                "$type", "app.bsky.feed.like" :> obj
                                "createdAt", DateTimeOffset.UtcNow.ToString("o")
                                "subject", dict [
                                    "cid", subject.cid
                                    "uri", subject.uri
                                ]
                            ]
                    ]
                }
                |> Requests.performRequestAsync httpClient
                |> Requests.thenReadAsync<MinimalRecord>
        }

        let DeleteRecordAsync httpClient (credentials: ICredentials) (collection: string) (rkey: string) =
            {
                method = HttpMethod.Post
                procedureName = "com.atproto.repo.deleteRecord"
                parameters = []
                credentials = credentials
                body = JsonBody [
                    "repo", credentials.DID
                    "collection", collection
                    "rkey", rkey
                ]
            }
            |> Requests.performRequestAsync httpClient
            |> Requests.thenIgnoreAsync

        type RepoDescription = {
            handle: string
            did: string
            didDoc: DIDDocument
            collections: string list
        }

        let DescribeRepoAsync httpClient credentials (repo: string) =
            {
                method = HttpMethod.Get
                procedureName = "com.atproto.repo.describeRepo"
                parameters = [
                    "repo", repo
                ]
                credentials = credentials
                body = NoBody
            }
            |> Requests.performRequestAsync httpClient
            |> Requests.thenReadAsync<RepoDescription>

        type RecordListItem<'T> = {
            uri: string
            cid: string
            value: 'T
        } with
            [<JsonIgnore>]
            member this.RecordKey =
                extractRecordKey this.uri

        type RecordList<'T> = {
            records: RecordListItem<'T> list
            cursor: string option
        }

        module Schemas =
            type BlobRef = {
                ``$link``: string
            }

            type Blob = {
                ref: BlobRef
                mimeType: string
                size: int
            }

            module Bluesky =
                module Feed =
                    type EmbedImage = {
                        alt: string option
                        image: Blob
                    } with
                        [<JsonIgnore>]
                        member this.Alt = this.alt |> Option.toObj
                        [<JsonIgnore>]
                        member this.BlobCID = this.image.ref.``$link``

                    type Embed = {
                        images: EmbedImage list option
                        record: MinimalRecord option
                    }

                    type Reply = {
                        parent: MinimalRecord
                        root: MinimalRecord
                    }

                    type Label = {
                        ``val``: string
                    }

                    type Labels = {
                        values: Label list
                    }

                    type Post = {
                        text: string
                        embed: Embed option
                        reply: Reply option
                        bridgyOriginalUrl: string option
                        labels: Labels option
                        createdAt: DateTimeOffset
                    } with
                        [<JsonIgnore>]
                        member this.Images =
                            this.embed
                            |> Option.bind (fun e -> e.images)
                            |> Option.defaultValue []
                        [<JsonIgnore>]
                        member this.EmbeddedRecord =
                            this.embed
                            |> Option.bind (fun e -> e.record)
                            |> Option.toObj
                        [<JsonIgnore>]
                        member this.InReplyTo =
                            Option.toObj this.reply
                        [<JsonIgnore>]
                        member this.Labels =
                            this.labels
                            |> Option.map (fun ls -> ls.values)
                            |> Option.defaultValue []
                            |> Seq.map (fun l -> l.``val``)
                        [<JsonIgnore>]
                        member this.ActivityPubUrl =
                            Option.toObj this.bridgyOriginalUrl

                    type Repost = {
                        createdAt: DateTimeOffset
                        subject: MinimalRecord
                    }

                module Actor =
                    type Profile = {
                        avatar: Blob option
                        displayName: string option
                        description: string option
                    } with
                        [<JsonIgnore>]
                        member this.AvatarCID = this.avatar |> Option.map (fun a -> a.ref.``$link``) |> Option.toObj
                        [<JsonIgnore>]
                        member this.DisplayName = Option.toObj this.displayName
                        [<JsonIgnore>]
                        member this.Description = Option.toObj this.description

        let GetRecordAsync<'T> httpClient credentials did collection rkey =
            {
                method = HttpMethod.Get
                procedureName = "com.atproto.repo.getRecord"
                parameters = [
                    "repo", did
                    "collection", collection
                    "rkey", rkey
                ]
                credentials = credentials
                body = NoBody
            }
            |> Requests.performRequestAsync httpClient
            |> Requests.thenReadAsync<'T>

        let ListRecordsAsync<'T> httpClient credentials did collection cursor =
            {
                method = HttpMethod.Get
                procedureName = "com.atproto.repo.listRecords"
                parameters = [
                    "repo", did
                    "collection", collection

                    match cursor with
                    | Some c -> "cursor", c
                    | None -> ()
                ]
                credentials = credentials
                body = NoBody
            }
            |> Requests.performRequestAsync httpClient
            |> Requests.thenReadAsync<RecordList<'T>>

        let EnumerateRecordsAsync<'T> httpClient credentials did collection = taskSeq {
            let mutable finished = false
            let mutable cursor = None
            while not finished do
                let! page = ListRecordsAsync<'T> httpClient credentials did collection cursor
                for item in page.records do item
                if List.isEmpty page.records then
                    finished <- true
                else
                    cursor <- page.cursor 
        }

        let EnumerateBlueskyFeedPostsAsync httpClient credentials did =
            EnumerateRecordsAsync<Schemas.Bluesky.Feed.Post> httpClient credentials did NSIDs.Bluesky.Feed.Post

        //let EnumerateBlueskyFeedRepostsAsync httpClient credentials did =
        //    EnumerateRecordsAsync<Schemas.Bluesky.Feed.Repost> httpClient credentials did NSIDs.Bluesky.Feed.Repost

        let EnumerateBlueskyActorProfilesAsync httpClient credentials did =
            EnumerateRecordsAsync<Schemas.Bluesky.Actor.Profile> httpClient credentials did NSIDs.Bluesky.Actor.Profile

        let GetBlobAsync httpClient credentials did cid = task {
            use! resp = Requests.performRequestAsync httpClient {
                method = HttpMethod.Get
                procedureName = "com.atproto.sync.getBlob"
                parameters = [
                    "did", did
                    "cid", cid
                ]
                credentials = credentials
                body = NoBody
            }

            use! stream = resp.EnsureSuccessStatusCode().Content.ReadAsStreamAsync()

            use ms = new System.IO.MemoryStream()
            do! stream.CopyToAsync(ms)

            return {|
                data = ms.ToArray()
                contentType =
                    resp.Content.Headers.ContentType
                    |> Option.ofObj
                    |> Option.map (fun c -> c.MediaType)
                    |> Option.defaultValue "application/octet-stream"
            |}
        }
