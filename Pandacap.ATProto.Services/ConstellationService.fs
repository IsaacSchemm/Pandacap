namespace Pandacap.ATProto.Services

open System
open System.Net.Http
open System.Net.Http.Json
open System.Threading
open FSharp.Control
open Pandacap.ATProto.Models
open Pandacap.ATProto.Services.Interfaces

module ConstellationService =
    let asyncReadAs (_: 'T) (resp: HttpContent) = async {
        let! token = Async.CancellationToken
        return! resp.ReadFromJsonAsync<'T>(token) |> Async.AwaitTask
    }

type ConstellationService(
    atProtoRequestHandler: IATProtoRequestHandler
) =
    let escape (str: string) = Uri.EscapeDataString(str)

    let asyncGetPage target collection path cursor = async {
        //client.Timeout <- TimeSpan.FromSeconds(3L)
        let qs = String.concat "&" [
            $"target={escape target}"
            $"collection={escape collection}"
            $"path={escape path}"
            match cursor with
            | None -> ()
            | Some cursor ->
                $"cursor={escape cursor}"
        ]
        //req.Headers.Accept.ParseAdd("application/json")
        let! token = Async.CancellationToken
        use cts = CancellationTokenSource.CreateLinkedTokenSource(token)
        cts.CancelAfter(TimeSpan.FromSeconds(3.0))
        use! resp =
            atProtoRequestHandler.GetJsonAsync(new Uri($"https://constellation.microcosm.blue/links?{qs}"), cts.Token)
            |> Async.AwaitTask
        return! resp.EnsureSuccessStatusCode().Content |> ConstellationService.asyncReadAs {|
            total = 0
            linking_records = [{|
                did = ""
                collection = ""
                rkey = ""
            |}]
            cursor = Some ""
        |}
    }

    interface IConstellationService with
        member _.GetLinksAsync(target, collection, path) = asyncSeq {
            let mutable cursor = None
            let mutable finished = false
            while not finished do
                let! page = asyncGetPage target collection path cursor
                for item in page.linking_records do
                    yield { Raw = $"at://{item.did}/{item.collection}/{item.rkey}" }

                cursor <- page.cursor

                if Option.isNone cursor || page.linking_records = [] then
                    finished <- true
        }
