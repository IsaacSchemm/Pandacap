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
} with
    interface ITokenPair with
        member this.AccessToken = this.accessJwt
        member this.RefreshToken = this.refreshJwt

/// Any object that can be used to authenticate with Bluesky.
/// Contains the user's DID, the PDS to connect to, and the current access token.
type ICredentials =
    abstract member PDS: string
    abstract member DID: string
    abstract member AccessToken: string

/// An object can implement this interface to enable automatic token refresh.
/// When a request fails, this library will use the refresh token to get a new
/// set of tokens, call UpdateTokensAsync to store them, and then retry.
type IAutomaticRefreshCredentials =
    inherit ICredentials
    inherit ITokenPair
    abstract member UpdateTokensAsync: newCredentials: ITokenPair -> Task

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

    let build (method: HttpMethod) (pds: string) (procedureName: string) (parameters: (string * string) seq) = {
        method = method
        uri = new Uri($"https://{Uri.EscapeDataString(pds)}/xrpc/{Uri.EscapeDataString(procedureName)}?{buildQueryString parameters}")
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
            build HttpMethod.Post req.uri.Host "com.atproto.server.refreshSession" []
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
        use! resp =
            Requester.build HttpMethod.Post hostname "com.atproto.server.createSession" []
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

    let readAsAsync<'T> httpClient credentials (_: 'T) req =
        readAsync<'T> httpClient credentials req

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
            Requester.build HttpMethod.Get credentials.PDS "app.bsky.notification.listNotifications" [
                match page with
                | FromCursor c -> "cursor", c
                | FromStart -> ()
            ]
            |> Reader.readAsync<NotificationList> httpClient (Some credentials)
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
            Requester.build HttpMethod.Post credentials.PDS "com.atproto.repo.uploadBlob" []
            |> Requester.addBody data contentType
            |> Reader.readAsAsync httpClient (Some credentials) {|
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

    type Post = {
        Text: string
        CreatedAt: DateTimeOffset
        Embed: PostEmbed
        PandacapMetadata: PandacapMetadata list
    }

    type ThreadGate = {
        Uri: string
    }

    type Record = Post of Post | EmptyThreadGate of ThreadGate

    type NewRecord = {
        uri: string
        cid: string
    } with
        member this.RecordKey = RecordKey.Extract this.uri

    let CreateRecordAsync httpClient (credentials: ICredentials) (record: Record) = task {
        return!
            Requester.build HttpMethod.Post credentials.PDS "com.atproto.repo.createRecord" []
            |> Requester.addJsonBody [
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
            ]
            |> Reader.readAsync<NewRecord> httpClient (Some credentials)
    }

    let DeleteRecordAsync httpClient (credentials: ICredentials) (rkey: string) =
        Requester.build HttpMethod.Post credentials.PDS "com.atproto.repo.deleteRecord" []
        |> Requester.addJsonBody [
            "repo", credentials.DID
            "collection", "app.bsky.feed.post"
            "rkey", rkey
        ]
        |> Reader.readAsync<unit> httpClient (Some credentials)
