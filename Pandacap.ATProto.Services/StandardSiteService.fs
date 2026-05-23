namespace Pandacap.ATProto.Services

open FSharp.Control
open Pandacap.ATProto.Services.Interfaces

type StandardSiteService(
    atProtoRequestHandler: IATProtoRequestHandler
) =
    let handler = atProtoRequestHandler

    interface IStandardSiteService with
        member _.GetNewestDocumentsAsync(pds, did) =
            StandardSiteRecords.Document.asyncFindNewestRecords handler pds did

        member _.GetDocumentAsync(pds, did, recordKey, cancellationToken) = Async.StartAsTask(
            StandardSiteRecords.Document.asyncGetRecord handler pds did recordKey,
            cancellationToken = cancellationToken)

        member _.GetPublicationAsync(pds, did, recordKey, cancellationToken) = Async.StartAsTask(
            StandardSiteRecords.Publication.asyncGetRecord handler pds did recordKey,
            cancellationToken = cancellationToken)
