namespace Pandacap.ATProto.Services

open FSharp.Control
open Pandacap.ATProto.Services.Interfaces

type BlueskyService(
    atProtoRequestHandler: IATProtoRequestHandler
) =
    let handler = atProtoRequestHandler

    interface IBlueskyService with
        member _.GetNewestLikesAsync(pds, did) =
            BlueskyRecords.BlueskyLike.asyncFindNewestRecords handler pds did

        member _.GetNewestPostsAsync(pds, did) =
            BlueskyRecords.BlueskyPost.asyncFindNewestRecords handler pds did

        member _.GetNewestRepostsAsync(pds, did) =
            BlueskyRecords.BlueskyRepost.asyncFindNewestRecords handler pds did

        member _.GetPostAsync(pds, did, recordKey, cancellationToken) = Async.StartAsTask(
            BlueskyRecords.BlueskyPost.asyncGetRecord handler pds did recordKey,
            cancellationToken = cancellationToken)

        member _.GetProfileAsync(pds, did, cancellationToken) = Async.StartAsTask(
            async {
                let! profile =
                    BlueskyRecords.BlueskyProfile.asyncFindNewestRecords handler pds did
                    |> AsyncSeq.tryHead

                return Option.toObj profile
            },
            cancellationToken = cancellationToken)
