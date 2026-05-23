namespace Pandacap.ATProto.Services

open FSharp.Control
open Pandacap.ATProto.Models

module internal Utility =
    let private toRecordAbstraction<'T, 'U> (translate: 'T -> 'U) (record: XRPC.Com.Atproto.Repo.Record<'T>) = {
        Ref = {
            CID = record.cid
            Uri = { Raw = record.uri }
        }
        Value = translate record.value
    }

    module Async =
        let toRecordAbstraction<'T, 'U> (translate: 'T -> 'U) (workflow: Async<XRPC.Com.Atproto.Repo.Record<'T>>) = async {
            let! record = workflow
            return record |> toRecordAbstraction translate
        }

    module AsyncSeq =
        let toRecordAbstractions<'T, 'U> (translate: 'T -> 'U) (sequence: AsyncSeq<XRPC.Com.Atproto.Repo.Record<'T>>) =
            sequence |> AsyncSeq.map (toRecordAbstraction translate)

    let findNewestItems handler pds did collection sample = asyncSeq {
        let listRecords = XRPC.Com.Atproto.Repo.asyncListRecords handler pds did collection 20

        let forward = XRPC.Com.Atproto.Repo.Forward
        let reverse = XRPC.Com.Atproto.Repo.Reverse

        let! forwardPage = listRecords None forward sample

        match forwardPage.records with
        | [] -> ()
        | [single] ->
            yield single
        | _ ->
            let! reversePage = listRecords None reverse sample

            let page =
                [forwardPage; reversePage]
                |> Seq.maxBy (fun page ->
                    page.records
                    |> Seq.map (fun r -> r.uri)
                    |> Seq.tryHead)

            yield! page.records

            let direction =
                if page = forwardPage
                then forward
                else reverse

            let mutable cursor = page.cursor

            while Option.isSome cursor do
                let! nextPage = listRecords cursor direction sample
                yield! nextPage.records
                cursor <- if List.isEmpty nextPage.records then None else nextPage.cursor
    }
