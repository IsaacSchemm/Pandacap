namespace Pandacap.ATProto.Services

open System
open Pandacap.ATProto.Services.Interfaces

type ATProtoService(
    atProtoRequestHandler: IATProtoRequestHandler
) =
    let handler = atProtoRequestHandler

    interface IATProtoService with
        member _.GetBlobAsync(pds, did, cid, cancellationToken) = Async.StartAsTask(
            XRPC.Com.Atproto.Repo.asyncGetBlob handler pds did cid,
            cancellationToken = cancellationToken)

        member _.GetCollectionsInRepoAsync(pds, did, cancellationToken) = Async.StartAsTask(
            async {
                let! repo = XRPC.Com.Atproto.Repo.asyncDescribeRepo handler pds did
                return repo.collections
            },
            cancellationToken = cancellationToken)

        member _.GetLastCommitCIDAsync(pds, did, cancellationToken) = Async.StartAsTask(
            async {
                let! commit = XRPC.Com.Atproto.Repo.asyncGetLatestCommit handler pds did
                return commit.cid
            },
            cancellationToken = cancellationToken)

        member _.GetRecordCreationTimeAsync(pds, did, collection, recordKey, cancellationToken) = Async.StartAsTask(
            async {
                let! record = XRPC.Com.Atproto.Repo.asyncGetRecord handler pds did collection recordKey {| createdAt = Some DateTimeOffset.MinValue |}
                return Option.toNullable record.value.createdAt
            },
            cancellationToken = cancellationToken)
