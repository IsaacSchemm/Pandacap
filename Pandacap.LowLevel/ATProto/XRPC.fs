namespace Pandacap.Clients.ATProto

open System
open System.Net.Http
open System.Net.Http.Headers
open System.Net.Http.Json
open System.Threading.Tasks

module XRPC =
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
        pds: string
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
                $"https://{request.pds}/xrpc/{Uri.EscapeDataString(request.procedureName)}?{queryString}")

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

            match error with
            | Some err ->
                return raise (XrpcException err)
            | None ->
                return resp.EnsureSuccessStatusCode()
        }

    module Com =
        module Atproto =
            module Identity =
                let ResolveHandleAsync httpClient pds handle =
                    Requests.sendAsync httpClient {
                        method = HttpMethod.Get
                        procedureName = "com.atproto.identity.resolveHandle"
                        parameters = [
                            "handle", handle
                        ]
                        pds = pds
                        body = NoBody
                    }
                    |> Requests.thenReadAsAsync {| did = "" |}

            module Repo =
                let DescribeRepoAsync httpClient pds (repo: string) =
                    {
                        method = HttpMethod.Get
                        procedureName = "com.atproto.repo.describeRepo"
                        parameters = [
                            "repo", repo
                        ]
                        pds = pds
                        body = NoBody
                    }
                    |> Requests.performRequestAsync httpClient
                    |> Requests.thenReadAsAsync {|
                        handle = ""
                        did = ""
                        collections = [""]
                    |}

                type Record<'T> = {
                    uri: string
                    cid: string
                    value: 'T
                }

                type Page<'T> = {
                    cursor: string option
                    records: Record<'T> list
                }

                let GetRecordAsync httpClient pds did collection rkey sample =
                    {
                        method = HttpMethod.Get
                        procedureName = "com.atproto.repo.getRecord"
                        parameters = [
                            "repo", did
                            "collection", collection
                            "rkey", rkey
                        ]
                        pds = pds
                        body = NoBody
                    }
                    |> Requests.performRequestAsync httpClient
                    |> Requests.thenReadAsAsync {
                        uri = ""
                        cid = ""
                        value = sample
                    }

                let ListRecordsAsync httpClient pds did collection limit cursor direction sample =
                    {
                        method = HttpMethod.Get
                        procedureName = "com.atproto.repo.listRecords"
                        parameters = [
                            "repo", did
                            "collection", collection

                            "limit", sprintf "%d" limit

                            match cursor with
                            | Some c -> "cursor", c
                            | None -> ()

                            match direction with
                            | Forward -> ()
                            | Reverse -> "reverse", "true"
                        ]
                        pds = pds
                        body = NoBody
                    }
                    |> Requests.performRequestAsync httpClient
                    |> Requests.thenReadAsAsync {
                        cursor = Some ""
                        records = [{
                            uri = ""
                            cid = ""
                            value = sample
                        }]
                    }

                let GetBlobAsync httpClient pds did cid = task {
                    use! resp = Requests.performRequestAsync httpClient {
                        method = HttpMethod.Get
                        procedureName = "com.atproto.sync.getBlob"
                        parameters = [
                            "did", did
                            "cid", cid
                        ]
                        pds = pds
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

                let GetLatestCommitAsync httpClient pds did =
                    {
                        method = HttpMethod.Get
                        procedureName = "com.atproto.sync.getLatestCommit"
                        parameters = [
                            "did", did
                        ]
                        pds = pds
                        body = NoBody
                    }
                    |> Requests.performRequestAsync httpClient
                    |> Requests.thenReadAsAsync {|
                        cid = ""
                        rev = ""
                    |}
