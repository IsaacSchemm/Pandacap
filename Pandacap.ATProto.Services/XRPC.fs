namespace Pandacap.ATProto.Services

open System
open System.Net.Http
open System.Net.Http.Json
open Pandacap.ATProto.Services.Interfaces

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
        procedureName: string
        parameters: (string * string) list
        pds: string
    }

    module private Requests =
        let send (handler: IATProtoRequestHandler) (request: Request): Async<HttpResponseMessage> = async {
            let queryString = String.concat "&" [
                for key, value in request.parameters do
                    $"{Uri.EscapeDataString(key)}={Uri.EscapeDataString(value)}"
            ]

            let url = $"https://{request.pds}/xrpc/{Uri.EscapeDataString(request.procedureName)}?{queryString}"

            let! token = Async.CancellationToken
            let! resp = handler.GetJsonAsync(new Uri(url), token) |> Async.AwaitTask

            let isJson =
                resp.Content.Headers.ContentType
                |> Option.ofObj
                |> Option.exists (fun c -> c.MediaType = "application/json")

            if not resp.IsSuccessStatusCode && isJson then
                let! err = resp.Content.ReadFromJsonAsync<XrpcError>(token) |> Async.AwaitTask
                return raise (XrpcException err)
            else
                return resp.EnsureSuccessStatusCode()
        }

        let thenReadAs (_: 'T) (t: Async<HttpResponseMessage>) = async {
            use! response = t

            let! token = Async.CancellationToken
            return! response.Content.ReadFromJsonAsync<'T>(token) |> Async.AwaitTask
        }

    module Com =
        module Atproto =
            module Identity =
                let internal asyncResolveHandle handler pds handle =
                    Requests.send handler {
                        procedureName = "com.atproto.identity.resolveHandle"
                        parameters = [
                            "handle", handle
                        ]
                        pds = pds
                    }
                    |> Requests.thenReadAs {| did = "" |}

            module Repo =
                let asyncDescribeRepo handler pds repo =
                    {
                        procedureName = "com.atproto.repo.describeRepo"
                        parameters = [
                            "repo", repo
                        ]
                        pds = pds
                    }
                    |> Requests.send handler
                    |> Requests.thenReadAs {|
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

                let internal asyncGetRecord handler pds did collection rkey sample =
                    {
                        procedureName = "com.atproto.repo.getRecord"
                        parameters = [
                            "repo", did
                            "collection", collection
                            "rkey", rkey
                        ]
                        pds = pds
                    }
                    |> Requests.send handler
                    |> Requests.thenReadAs {
                        uri = ""
                        cid = ""
                        value = sample
                    }

                type ListDirection = Forward | Reverse

                let internal asyncListRecords handler pds did collection limit cursor direction sample =
                    {
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
                    }
                    |> Requests.send handler
                    |> Requests.thenReadAs {
                        cursor = Some ""
                        records = [{
                            uri = ""
                            cid = ""
                            value = sample
                        }]
                    }

                let internal asyncGetBlob handler pds did cid = async {
                    use! resp = Requests.send handler {
                        procedureName = "com.atproto.sync.getBlob"
                        parameters = [
                            "did", did
                            "cid", cid
                        ]
                        pds = pds
                    }

                    let! token = Async.CancellationToken
                    use! stream = resp.EnsureSuccessStatusCode().Content.ReadAsStreamAsync(token) |> Async.AwaitTask

                    use ms = new System.IO.MemoryStream()
                    do! stream.CopyToAsync(ms, token) |> Async.AwaitTask

                    let data = ms.ToArray()
                    let contentType =
                        resp.Content.Headers.ContentType
                        |> Option.ofObj
                        |> Option.map (fun c -> c.MediaType)
                        |> Option.defaultValue "application/octet-stream"

                    return {
                        new IATProtoBlob with
                            member _.Data = data
                            member _.ContentType = contentType
                    }
                }

                let internal asyncGetLatestCommit handler pds did =
                    {
                        procedureName = "com.atproto.sync.getLatestCommit"
                        parameters = [
                            "did", did
                        ]
                        pds = pds
                    }
                    |> Requests.send handler
                    |> Requests.thenReadAs {|
                        cid = ""
                        rev = ""
                    |}
