namespace Pandacap.Clients.ATProto

open System
open System.Net.Http
open System.Net.Http.Headers
open System.Net.Http.Json
open System.Threading.Tasks

module XRPC =
    type IHost =
        abstract member PDS: string

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
        abstract member UpdateTokensAsync: newCredentials: ATProtoTokens -> Task

    type XrpcError = {
        error: string
        message: string
    }

    exception XrpcException of XrpcError

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

    module internal Requests =
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

        let thenIgnoreAsync (t: Task<HttpResponseMessage>) = task {
            use! response = t
            ignore response
        }

        let thenReadAsAsync (_: 'T) (t: Task<HttpResponseMessage>) = task {
            use! response = t
            return! response.Content.ReadFromJsonAsync<'T>()
        }

        let thenMapAsync (f: 'T -> 'U) (t: Task<'T>) = task {
            let! o = t
            return f o
        }

        let rec performRequestAsync (httpClient: HttpClient) (req: Request): Task<HttpResponseMessage> = task {
            let! resp = sendAsync httpClient req

            let! error = task {
                let isJson =
                    resp.Content.Headers.ContentType
                    |> Option.ofObj
                    |> Option.exists (fun c -> c.MediaType = "application/json")
                if not resp.IsSuccessStatusCode && isJson then
                    let! err = resp.Content.ReadFromJsonAsync<XrpcError>()
                    return Some err
                else
                    return None
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

                let! tokens =
                    tokenResponse.EnsureSuccessStatusCode()
                    |> Task.FromResult
                    |> thenReadAsAsync {|
                        accessJwt = ""
                        refreshJwt = ""
                        handle = ""
                        did = ""
                    |}
                    |> thenMapAsync (fun x -> {
                        AccessToken = x.accessJwt
                        RefreshToken = x.refreshJwt
                        Handle = x.handle
                        DID = x.did
                    })

                do! credentials.UpdateTokensAsync(tokens)

                return! performRequestAsync httpClient {
                    req with
                        credentials = {
                            new IToken with
                                member _.PDS = credentials.PDS
                                member _.Token = tokens.AccessToken
                        }
                }
            | Some err, _ ->
                return raise (XrpcException err)
            | None, _ ->
                return resp.EnsureSuccessStatusCode()
        }

    module Com =
        module Atproto =
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

            module Repo =
                let UploadBlobAsync httpClient (credentials: ICredentials) data contentType =
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

                let CreateRecordAsync httpClient (credentials: ICredentials) (record: ATProtoCreateParameters) =
                    {
                        method = HttpMethod.Post
                        procedureName = "com.atproto.repo.createRecord"
                        parameters = []
                        credentials = credentials
                        body = JsonBody [
                            "repo", credentials.DID

                            match record with
                            | BlueskyPost post ->
                                "collection", NSIDs.App.Bsky.Feed.Post
                                "record", dict [
                                    "$type", NSIDs.App.Bsky.Feed.Post :> obj
                                    "text", post.Text
                                    "createdAt", post.CreatedAt.ToString("o")

                                    for r in post.InReplyTo do
                                        "reply", dict [
                                            "root", dict [
                                                "cid", r.Root.CID
                                                "uri", r.Root.Uri.Raw
                                            ]
                                            "parent", dict [
                                                "cid", r.Parent.CID
                                                "uri", r.Parent.Uri.Raw
                                            ]
                                        ]

                                    match Option.ofNullable post.PandacapPost with
                                    | Some id -> "pandacapPost", id
                                    | None -> ()

                                    if not (List.isEmpty post.Images) then
                                        "embed", dict [
                                            "$type", "app.bsky.embed.images" :> obj
                                            "images", [
                                                for i in post.Images do dict [
                                                    "image", i.Blob
                                                    "alt", i.Alt

                                                    if i.Width > 0 && i.Height > 0 then
                                                        "aspectRatio", dict [
                                                            "width", i.Width
                                                            "height", i.Height
                                                        ]
                                                ]
                                            ]
                                        ]
                                ]

                            | BlueskyLike subject ->
                                "collection", NSIDs.App.Bsky.Feed.Like
                                "record", dict [
                                    "$type", NSIDs.App.Bsky.Feed.Like :> obj
                                    "createdAt", DateTimeOffset.UtcNow.ToString("o")
                                    "subject", dict [
                                        "cid", subject.CID
                                        "uri", subject.Uri.Raw
                                    ]
                                ]
                        ]
                    }
                    |> Requests.performRequestAsync httpClient
                    |> Requests.thenReadAsAsync {|
                        cid = ""
                        uri = ""
                    |}
                    |> Requests.thenMapAsync (fun r -> {
                        CID = r.cid
                        Uri = { Raw = r.uri }
                    })

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
                    |> Requests.thenReadAsAsync {|
                        handle = ""
                        did = ""
                        collections = [""]
                    |}

                let private getRecordAsync httpClient credentials did collection rkey sample =
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
                    |> Requests.thenReadAsAsync {|
                        uri = ""
                        cid = ""
                        value = sample
                    |}

                let private listRecordsAsync httpClient credentials did collection limit cursor direction sample =
                    {
                        method = HttpMethod.Get
                        procedureName = "com.atproto.repo.listRecords"
                        parameters = [
                            "repo", did
                            "collection", collection

                            "limit", sprintf "%d" limit

                            if not (isNull cursor) then
                                "cursor", cursor

                            match direction with
                            | Forward -> ()
                            | Reverse -> "reverse", "true"
                        ]
                        credentials = credentials
                        body = NoBody
                    }
                    |> Requests.performRequestAsync httpClient
                    |> Requests.thenReadAsAsync {|
                        cursor = Some ""
                        records = [{|
                            uri = ""
                            cid = ""
                            value = sample
                        |}]
                    |}

                module BlueskyProfile =
                    let ListRecordsAsync httpClient credentials did limit cursor direction =
                        listRecordsAsync httpClient credentials did NSIDs.App.Bsky.Actor.Profile limit cursor direction {|
                            avatar = Some {|
                                ref = Some {|
                                    ``$link`` = ""
                                |}
                                mimeType = ""
                                size = Some 0
                                cid = Some ""
                            |}
                            displayName = Some ""
                            description = Some ""
                        |}
                        |> Requests.thenMapAsync (fun l -> {
                            Cursor = Option.toObj l.cursor
                            Items = [
                                for x in l.records do {
                                    Ref = {
                                        CID = x.cid
                                        Uri = { Raw = x.uri }
                                    }
                                    Value = {
                                        AvatarCID =
                                            x.value.avatar
                                            |> Option.bind (fun a ->
                                                a.ref
                                                |> Option.map (fun r -> r.``$link``)
                                                |> Option.orElse a.cid)
                                            |> Option.toObj
                                        DisplayName = Option.toObj x.value.displayName
                                        Description = Option.toObj x.value.description
                                    }
                                }
                            ]
                        })

                module BlueskyPost =
                    let GetRecordAsync httpClient credentials did rkey =
                        getRecordAsync httpClient credentials did NSIDs.App.Bsky.Feed.Post rkey {|
                            text = ""
                            embed = Some {|
                                images = Some [{|
                                    alt = Some ""
                                    image = {|
                                        ref = Some {|
                                            ``$link`` = ""
                                        |}
                                        mimeType = ""
                                        size = Some 0
                                        cid = Some ""
                                    |}
                                |}]
                                record = Some {|
                                    cid = ""
                                    uri = ""
                                |}
                            |}
                            reply = Some {|
                                parent = {|
                                    cid = ""
                                    uri = ""
                                |}
                                root = {|
                                    cid = ""
                                    uri = ""
                                |}
                            |}
                            bridgyOriginalUrl = Some ""
                            labels = Some {|
                                values = [{|
                                    ``val`` = ""
                                |}]
                            |}
                            createdAt = DateTimeOffset.MinValue
                        |}
                        |> Requests.thenMapAsync (fun x -> {
                            Ref = {
                                CID = x.cid
                                Uri = { Raw = x.uri }
                            }
                            Value = {
                                Text = x.value.text
                                Images =
                                    x.value.embed
                                    |> Option.bind (fun e -> e.images)
                                    |> Option.defaultValue []
                                    |> List.map (fun image -> {
                                        CID =
                                            image.image.ref
                                            |> Option.map (fun r -> r.``$link``)
                                            |> Option.orElse image.image.cid
                                            |> Option.get
                                        Alt = image.alt |> Option.defaultValue ""
                                    })
                                Quoted =
                                    x.value.embed
                                    |> Option.bind (fun e -> e.record)
                                    |> Option.map (fun r -> {
                                        CID = r.cid
                                        Uri = { Raw = r.uri }
                                    })
                                    |> Option.toList
                                InReplyTo =
                                    x.value.reply
                                    |> Option.map (fun r -> {
                                        Parent = {
                                            CID = r.parent.cid
                                            Uri = { Raw = r.parent.uri }
                                        }
                                        Root = {
                                            CID = r.root.cid
                                            Uri = { Raw = r.root.uri }
                                        }
                                    })
                                    |> Option.toList
                                BridgyOriginalUrl = Option.toObj x.value.bridgyOriginalUrl
                                Labels =
                                    x.value.labels
                                    |> Option.map (fun l -> l.values)
                                    |> Option.defaultValue []
                                    |> List.map (fun v -> v.``val``)
                                CreatedAt = x.value.createdAt
                            }
                        })

                    let ListRecordsAsync httpClient credentials did limit cursor direction =
                        listRecordsAsync httpClient credentials did NSIDs.App.Bsky.Feed.Post limit cursor direction {|
                            text = ""
                            embed = Some {|
                                images = Some [{|
                                    alt = Some ""
                                    image = {|
                                        ref = Some {|
                                            ``$link`` = ""
                                        |}
                                        mimeType = ""
                                        size = Some 0
                                        cid = Some ""
                                    |}
                                |}]
                                record = Some {|
                                    cid = ""
                                    uri = ""
                                |}
                            |}
                            reply = Some {|
                                parent = {|
                                    cid = ""
                                    uri = ""
                                |}
                                root = {|
                                    cid = ""
                                    uri = ""
                                |}
                            |}
                            bridgyOriginalUrl = Some ""
                            labels = Some {|
                                values = [{|
                                    ``val`` = ""
                                |}]
                            |}
                            createdAt = DateTimeOffset.MinValue
                        |}
                        |> Requests.thenMapAsync (fun l -> {
                            Cursor = Option.toObj l.cursor
                            Items = [
                                for x in l.records do {
                                    Ref = {
                                        CID = x.cid
                                        Uri = { Raw = x.uri }
                                    }
                                    Value = {
                                        Text = x.value.text
                                        Images =
                                            x.value.embed
                                            |> Option.bind (fun e -> e.images)
                                            |> Option.defaultValue []
                                            |> List.map (fun image -> {
                                                CID =
                                                    image.image.ref
                                                    |> Option.map (fun r -> r.``$link``)
                                                    |> Option.orElse image.image.cid
                                                    |> Option.get
                                                Alt = image.alt |> Option.defaultValue ""
                                            })
                                        Quoted =
                                            x.value.embed
                                            |> Option.bind (fun e -> e.record)
                                            |> Option.map (fun r -> {
                                                CID = r.cid
                                                Uri = { Raw = r.uri }
                                            })
                                            |> Option.toList
                                        InReplyTo =
                                            x.value.reply
                                            |> Option.map (fun r -> {
                                                Parent = {
                                                    CID = r.parent.cid
                                                    Uri = { Raw = r.parent.uri }
                                                }
                                                Root = {
                                                    CID = r.root.cid
                                                    Uri = { Raw = r.root.uri }
                                                }
                                            })
                                            |> Option.toList
                                        BridgyOriginalUrl = Option.toObj x.value.bridgyOriginalUrl
                                        Labels =
                                            x.value.labels
                                            |> Option.map (fun l -> l.values)
                                            |> Option.defaultValue []
                                            |> List.map (fun v -> v.``val``)
                                        CreatedAt = x.value.createdAt
                                    }
                                }
                            ]
                        })

                module BlueskyLike =
                    let ListRecordsAsync httpClient credentials did limit cursor direction =
                        listRecordsAsync httpClient credentials did NSIDs.App.Bsky.Feed.Like limit cursor direction {|
                            createdAt = DateTimeOffset.MinValue
                            subject = {|
                                uri = ""
                                cid = ""
                            |}
                        |}
                        |> Requests.thenMapAsync (fun l -> {
                            Cursor = Option.toObj l.cursor
                            Items = [
                                for x in l.records do {
                                    Ref = {
                                        CID = x.cid
                                        Uri = { Raw = x.uri }
                                    }
                                    Value = {
                                        CreatedAt = x.value.createdAt
                                        Subject = {
                                            CID = x.value.subject.cid
                                            Uri = { Raw = x.value.subject.uri }
                                        }
                                    }
                                }
                            ]
                        })

                module BlueskyRepost =
                    let ListRecordsAsync httpClient credentials did limit cursor direction =
                        listRecordsAsync httpClient credentials did NSIDs.App.Bsky.Feed.Repost limit cursor direction {|
                            createdAt = DateTimeOffset.MinValue
                            subject = {|
                                uri = ""
                                cid = ""
                            |}
                        |}
                        |> Requests.thenMapAsync (fun l -> {
                            Cursor = Option.toObj l.cursor
                            Items = [
                                for x in l.records do {
                                    Ref = {
                                        CID = x.cid
                                        Uri = { Raw = x.uri }
                                    }
                                    Value = {
                                        CreatedAt = x.value.createdAt
                                        Subject = {
                                            CID = x.value.subject.cid
                                            Uri = { Raw = x.value.subject.uri }
                                        }
                                    }
                                }
                            ]
                        })

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
                        Data = ms.ToArray()
                        ContentType =
                            resp.Content.Headers.ContentType
                            |> Option.ofObj
                            |> Option.map (fun c -> c.MediaType)
                            |> Option.defaultValue "application/octet-stream"
                    |}
                }

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

                    return! resp.EnsureSuccessStatusCode()
                        |> Task.FromResult
                        |> Requests.thenReadAsAsync {|
                            accessJwt = ""
                            refreshJwt = ""
                            handle = ""
                            did = ""
                        |}
                        |> Requests.thenMapAsync (fun x -> {
                            AccessToken = x.accessJwt
                            RefreshToken = x.refreshJwt
                            Handle = x.handle
                            DID = x.did
                        })
                }

    module App =
        module Bsky =
            module Notification =
                let ListNotificationsAsync httpClient credentials cursor =
                    {
                        method = HttpMethod.Get
                        procedureName = "app.bsky.notification.listNotifications"
                        parameters = [
                            if not (isNull cursor) then
                                "cursor", cursor
                        ]
                        credentials = credentials
                        body = NoBody
                    }
                    |> Requests.performRequestAsync httpClient
                    |> Requests.thenReadAsAsync {|
                        cursor = Some ""
                        notifications = [{|
                            uri = ""
                            cid = ""
                            author = {|
                                did = ""
                                handle = ""
                                displayName = Some ""
                            |}
                            reason = ""
                            reasonSubject = ""
                            isRead = false
                            indexedAt = DateTimeOffset.MinValue
                        |}]
                    |}
                    |> Requests.thenMapAsync (fun l -> {
                        Cursor = Option.toObj l.cursor
                        Items = [
                            for x in l.notifications do {
                                Ref = {
                                    CID = x.cid
                                    Uri = { Raw = x.uri }
                                }
                                Author = {
                                    DID = x.author.did
                                    Handle = x.author.handle
                                    DisplayName = Option.toObj x.author.displayName
                                }
                                Reason = x.reason
                                ReasonSubject = { Raw = x.reasonSubject }
                                IsRead = x.isRead
                                IndexedAt = x.indexedAt
                            }
                        ]
                    })
