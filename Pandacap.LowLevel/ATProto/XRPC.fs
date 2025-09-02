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
        abstract member UpdateTokensAsync: newCredentials: Lexicon.ITokens -> Task

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

                tokenResponse.EnsureSuccessStatusCode() |> ignore

                let! tokens = tokenResponse.Content.ReadFromJsonAsync<Lexicon.Com.Atproto.Server.RefreshSession>()
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
                return resp.EnsureSuccessStatusCode()
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

                type EmbeddedImage = {
                    Blob: obj
                    Alt: string
                    Width: int
                    Height: int
                }

                type EmbeddedContent = Images of EmbeddedImage list | NoEmbed

                type PostParameters = {
                    Text: string
                    CreatedAt: DateTimeOffset
                    Embed: EmbeddedContent
                    InReplyTo: Lexicon.App.Bsky.Feed.Post.Reply list
                    PandacapPost: Nullable<Guid>
                }

                type RecordToCreate =
                | Post of PostParameters
                | Like of Lexicon.Com.Atproto.Repo.StrongRef

                let CreateRecordAsync httpClient (credentials: ICredentials) record =
                    {
                        method = HttpMethod.Post
                        procedureName = "com.atproto.repo.createRecord"
                        parameters = []
                        credentials = credentials
                        body = JsonBody [
                            "repo", credentials.DID

                            match record with
                            | Post post ->
                                "collection", NSIDs.App.Bsky.Feed.Post
                                "record", dict [
                                    "$type", NSIDs.App.Bsky.Feed.Post :> obj
                                    "text", post.Text
                                    "createdAt", post.CreatedAt.ToString("o")

                                    for r in post.InReplyTo do
                                        "reply", dict [
                                            "root", r.root
                                            "parent", r.parent
                                        ]

                                    match Option.ofNullable post.PandacapPost with
                                    | Some id -> "pandacapPost", id
                                    | None -> ()

                                    match post.Embed with
                                    | NoEmbed -> ()
                                    | Images images ->
                                        "embed", dict [
                                            "$type", "app.bsky.embed.images" :> obj
                                            "images", [
                                                for i in images do dict [
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

                            | Like subject ->
                                "collection", NSIDs.App.Bsky.Feed.Like
                                "record", dict [
                                    "$type", NSIDs.App.Bsky.Feed.Like :> obj
                                    "createdAt", DateTimeOffset.UtcNow.ToString("o")
                                    "subject", dict [
                                        "cid", subject.cid
                                        "uri", subject.uri
                                    ]
                                ]
                        ]
                    }
                    |> Requests.performRequestAsync httpClient
                    |> Requests.thenReadAsync<Lexicon.Com.Atproto.Repo.StrongRef>

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
                    |> Requests.thenReadAsync<Lexicon.Com.Atproto.Repo.GetRecord<'T>>

                type Direction = Forward | Reverse

                let ListRecordsAsync<'T> httpClient credentials did collection limit cursor direction =
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
                    |> Requests.thenReadAsync<Lexicon.Com.Atproto.Repo.ListRecords<'T>>

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

                    return! resp.EnsureSuccessStatusCode().Content.ReadFromJsonAsync<Lexicon.Com.Atproto.Server.RefreshSession>()
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
                    |> Requests.thenReadAsync<Lexicon.App.Bsky.Notification.ListNotifications>
