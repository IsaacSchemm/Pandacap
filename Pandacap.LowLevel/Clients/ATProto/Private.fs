namespace Pandacap.Clients.ATProto.Private

open System
open System.Net
open System.Net.Http
open System.Net.Http.Headers
open System.Net.Http.Json
open System.Threading.Tasks

module RecordKey =
    let Extract (uri: string) =
        match uri with
        | null -> null
        | _ -> uri.Split('/') |> Array.last |> Uri.UnescapeDataString

type Page =
| FromStart
| FromCursor of string

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
}

/// An object that can be used to authenticate with Bluesky.
type ICredentials =
    abstract member PDS: string
    abstract member DID: string
    abstract member AccessToken: string
    abstract member RefreshToken: string
    abstract member UpdateTokensAsync: newCredentials: Tokens -> Task

type RequestCredentials =
| RequestCredentials of ICredentials
| Token of string
| NoCredentials

type Body =
| NoBody
| JsonBody of (string * obj) list
| RawBody of data: byte[] * contentType: string

type Request = {
    method: HttpMethod
    pds: string
    procedureName: string
    parameters: (string * string) list
    credentials: RequestCredentials
    body: Body
}

module Requests =
    type XrpcError = {
        error: string
        message: string
    }

    exception XrpcException of XrpcError

    let sendAsync (httpClient: HttpClient) (request: Request) = task {
        let queryString = String.concat "&" [
            for key, value in request.parameters do
                $"{Uri.EscapeDataString(key)}={Uri.EscapeDataString(value)}"
        ]

        use req = new HttpRequestMessage(
            request.method,
            $"https://{request.pds}/xrpc/{Uri.EscapeDataString(request.procedureName)}?{queryString}")

        match request.credentials with
        | RequestCredentials c -> req.Headers.Authorization <- new AuthenticationHeaderValue("Bearer", c.AccessToken)
        | Token t -> req.Headers.Authorization <- new AuthenticationHeaderValue("Bearer", t)
        | NoCredentials -> ()

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
        | Some err, RequestCredentials credentials when err.error = "ExpiredToken" && not (String.IsNullOrEmpty(credentials.RefreshToken)) ->
            use! tokenResponse = sendAsync httpClient {
                method = HttpMethod.Post
                pds = req.pds
                procedureName = "com.atproto.server.refreshSession"
                parameters = []
                credentials = Token credentials.RefreshToken
                body = NoBody
            }

            tokenResponse.EnsureSuccessStatusCode() |> ignore

            let! tokens = tokenResponse.Content.ReadFromJsonAsync<Tokens>()
            do! credentials.UpdateTokensAsync(tokens)

            return! performRequestAsync httpClient { req with credentials = Token tokens.accessJwt }
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

/// Handles logging in and refreshing tokens.
module Auth =
    let CreateSessionAsync httpClient hostname identifier password = task {
        use! resp = Requests.sendAsync httpClient {
            method = HttpMethod.Post
            pds = hostname
            procedureName = "com.atproto.server.createSession"
            parameters = []
            credentials = NoCredentials
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

module Bluesky =
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
        } with
            member this.RecordKey = RecordKey.Extract this.uri
            member this.ReasonSubjectRecordKey = RecordKey.Extract this.reasonSubject

        type NotificationList = {
            cursor: string option
            notifications: Notification list
        } with
            member this.NextPage =
                this.cursor
                |> Option.map FromCursor
                |> Option.toList

        let ListNotificationsAsync httpClient (credentials: ICredentials) page = task {
            return!
                {
                    method = HttpMethod.Get
                    pds = credentials.PDS
                    procedureName = "app.bsky.notification.listNotifications"
                    parameters = [
                        match page with
                        | FromCursor c -> "cursor", c
                        | FromStart -> ()
                    ]
                    credentials = RequestCredentials credentials
                    body = NoBody
                }
                |> Requests.performRequestAsync httpClient
                |> Requests.thenReadAsync<NotificationList>
        }

/// Handles creating and deleting records in the repo, e.g. Bluesky posts.
module Repo =
    type PostImage = {
        Blob: obj
        AltText: string
        Dimensions: (int * int) option
    }

    type PostExternal = {
        Description: string
        Blob: obj
        Title: string
        Uri: string
    }

    let UploadBlobAsync httpClient (credentials: ICredentials) (data: byte[]) (contentType: string) (alt: string) = task {
        let! blobResponse =
            {
                method = HttpMethod.Post
                pds = credentials.PDS
                procedureName = "com.atproto.repo.uploadBlob"
                parameters = []
                credentials = RequestCredentials credentials
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

    type PostEmbed = Images of PostImage list | External of PostExternal | NoEmbed

    type PandacapMetadata = PostId of Guid | FavoriteId of string

    type MinimalRecord = {
        uri: string
        cid: string
    } with
        member this.RecordKey = RecordKey.Extract this.uri

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

    type Record =
    | Post of Post
    | EmptyThreadGate of ThreadGate
    | Like of MinimalRecord

    let CreateRecordAsync httpClient (credentials: ICredentials) (record: Record) = task {
        return!
            {
                method = HttpMethod.Post
                pds = credentials.PDS
                procedureName = "com.atproto.repo.createRecord"
                parameters = []
                credentials = RequestCredentials credentials
                body = JsonBody [
                    "repo", credentials.DID

                    match record with
                    | EmptyThreadGate x ->
                        "collection", "app.bsky.feed.threadgate"
                        "rkey", RecordKey.Extract x.Uri
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
                                | FavoriteId id ->
                                    "pandacapFavorite", id

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
            pds = credentials.PDS
            procedureName = "com.atproto.repo.deleteRecord"
            parameters = []
            credentials = RequestCredentials credentials
            body = JsonBody [
                "repo", credentials.DID
                "collection", collection
                "rkey", rkey
            ]
        }
        |> Requests.performRequestAsync httpClient
        |> Requests.thenIgnoreAsync
