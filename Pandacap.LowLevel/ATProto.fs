namespace Pandacap.LowLevel.ATProto

open System
open System.Net
open System.Net.Http
open System.Net.Http.Headers
open System.Net.Http.Json
open System.Threading.Tasks
open FSharp.Control

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

    let build (host: IHost) (method: HttpMethod) (procedureName: string) = {
        method = method
        uri = new Uri($"https://{Uri.EscapeDataString(host.PDS)}/xrpc/{Uri.EscapeDataString(procedureName)}")
        bearerToken = None
        body = NoBody
    }

    let buildQueryString (parameters: (string * string) seq) = String.concat "&" [
        for key, value in parameters do
            $"{Uri.EscapeDataString(key)}={Uri.EscapeDataString(value)}"
    ]

    let addQueryParameters (parameters: (string * string) seq) (req: Request) = {
        req with uri = new Uri($"{req.uri.GetLeftPart(UriPartial.Path)}?{buildQueryString parameters}")
    }

    let addJsonBody (body: (string * obj) list) (req: Request) = {
        req with body = JsonBody body
    }

    let addBody (body: byte[]) (contentType: string) (req: Request) = {
        req with body = RawBody (body, contentType)
    }

    let addAccessToken (credentials: ICredentials) (req: Request) = {
        req with bearerToken = Some credentials.AccessToken
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

/// Handles logging in and refreshing tokens.
module Auth =
    let CreateSessionAsync httpClient hostname identifier password = task {
        let host = { new IHost with member _.PDS = hostname }

        use! resp =
            Requester.build host HttpMethod.Post "com.atproto.server.createSession"
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

    let RefreshSessionAsync httpClient credentials = task {
        use! resp =
            Requester.build credentials HttpMethod.Post "com.atproto.server.refreshSession"
            |> Requester.addRefreshToken credentials
            |> Requester.sendAsync httpClient
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

    let readAsync<'T> httpClient (credentials: ICredentials) req = task {
        use! initialResp =
            req
            |> Requester.addAccessToken credentials
            |> Requester.sendAsync httpClient

        use! finalResp = task {
            match credentials, initialResp.StatusCode with
            | :? IAutomaticRefreshCredentials as auto, HttpStatusCode.BadRequest ->
                let! err = initialResp.Content.ReadFromJsonAsync<Error>()
                if err.error = "ExpiredToken" then
                    let! newCredentials = Auth.RefreshSessionAsync httpClient auto
                    do! auto.UpdateTokensAsync(newCredentials)
                    return!
                        req
                        |> Requester.addAccessToken credentials
                        |> Requester.sendAsync httpClient
                else
                    return raise (ErrorException err)
            | _ ->
                return initialResp
        }

        finalResp.EnsureSuccessStatusCode() |> ignore

        let! str = finalResp.Content.ReadAsStringAsync()
        printfn "%s" str

        if typedefof<'T> = typedefof<unit> then
            return () :> obj :?> 'T
        else
            return! finalResp.Content.ReadFromJsonAsync<'T>()
    }

/// Lists notifications on the user's Bluesky account.
module Notifications =
    type Page =
    | FromStart
    | FromCursor of string

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
    }

    let ListNotificationsAsync httpClient credentials page = task {
        return!
            Requester.build credentials HttpMethod.Get "app.bsky.notification.listNotifications"
            |> Requester.addQueryParameters [
                match page with
                | FromCursor c -> "cursor", c
                | FromStart -> ()
            ]
            |> Reader.readAsync<NotificationList> httpClient credentials
    }

    //let ListAllNotificationsAsync httpClient credentials = taskSeq {
    //    let mutable page = FromStart
    //    let mutable finished = false

    //    while not finished do
    //        let! result = ListNotificationsAsync httpClient credentials page
    //        yield! result.notifications

    //        match result.cursor with
    //        | Some nextCursor ->
    //            page <- FromCursor nextCursor
    //        | None ->
    //            finished <- true
    //}

/// Handles creating and deleting Bluesky posts.
module Repo =
    type BlobResponse = {
        blob: obj
    }

    type BlobWithAltText = {
        blob: obj
        alt: string
    }

    let UploadBlobAsync httpClient (credentials: ICredentials) (data: byte[]) (contentType: string) (alt: string) = task {
        let! blobResponse =
            Requester.build credentials HttpMethod.Post "com.atproto.repo.uploadBlob"
            |> Requester.addBody data contentType
            |> Reader.readAsync<BlobResponse> httpClient credentials
        return { blob = blobResponse.blob; alt = alt }
    }

    type Post = {
        text: string
        createdAt: DateTimeOffset
        images: BlobWithAltText seq
    }

    type NewRecord = {
        uri: string
        cid: string
    } with
        member this.RecordKey =
            this.uri.Split('/')
            |> Seq.last

    let CreateRecordAsync httpClient (credentials: ICredentials) (post: Post) = task {
        return!
            Requester.build credentials HttpMethod.Post "com.atproto.repo.createRecord"
            |> Requester.addJsonBody [
                "repo", credentials.DID
                "collection", "app.bsky.feed.post"
                "record", dict [
                    "$type", "app.bsky.feed.post" :> obj
                    "text", post.text
                    "createdAt", post.createdAt.ToString("o")

                    if not (Seq.isEmpty post.images) then
                        "embed", dict [
                            "$type", "app.bsky.embed.images" :> obj
                            "images", [
                                for i in post.images do dict [
                                    "image", i.blob
                                    "alt", i.alt
                                ]
                            ]
                        ]
                ]
            ]
            |> Reader.readAsync<NewRecord> httpClient credentials
    }

    let DeleteRecordAsync httpClient (credentials: ICredentials) (rkey: string) = task {
        do!
            Requester.build credentials HttpMethod.Post "com.atproto.repo.deleteRecord"
            |> Requester.addJsonBody [
                "repo", credentials.DID
                "collection", "app.bsky.feed.post"
                "rkey", rkey
            ]
            |> Reader.readAsync<unit> httpClient credentials
    }
