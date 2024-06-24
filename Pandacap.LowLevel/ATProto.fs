namespace Pandacap.LowLevel.ATProto

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

/// Handles requests within app.bsky.feed.
module BlueskyFeed =
    type Page =
    | FromStart
    | FromCursor of string

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

    type Embed = {
        images: Image list option
    }

    type Record = {
        createdAt: DateTimeOffset
        text: string
    }

    type Post = {
        uri: string
        cid: string
        author: Author
        record: Record
        embed: Embed option
        indexedAt: DateTimeOffset
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
        member this.IsRepost =
            match this.reason with
            | Some r when r.``$type`` = "app.bsky.feed.defs#reasonRepost" -> true
            | _ -> false

    type FeedResponse = {
        cursor: string option
        feed: FeedItem list
    }

    let GetTimelineAsync httpClient credentials page = task {
        return!
            Requester.build credentials HttpMethod.Get "app.bsky.feed.getTimeline"
            |> Requester.addQueryParameters [
                match page with
                | FromCursor c -> "cursor", c
                | FromStart -> ()
            ]
            |> Reader.readAsync<FeedResponse> httpClient credentials
    }
