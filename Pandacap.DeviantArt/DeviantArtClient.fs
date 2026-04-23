namespace Pandacap.DeviantArt

open System
open FSharp.Control
open DeviantArtFs.ParameterTypes
open Pandacap.Credentials.Interfaces
open Pandacap.DeviantArt.Interfaces

type internal DeviantArtClient(
    deviantArtCredentialProvider: IDeviantArtCredentialProvider
) =
    let asyncGetToken () = async {
        let! token = deviantArtCredentialProvider.GetTokensAsync() |> AsyncSeq.tryHead
        
        match token with
        | Some t -> return t
        | None -> return failwith "No DeviantArt account connected"
    }

    let wrap (item: DeviantArtFs.ResponseTypes.Deviation) = {
        new IDeviation with
            member _.Title = Option.toObj item.title
            member _.DeviationId = item.deviationid
            member _.Url = Option.toObj item.url
            member _.PublishedTime = Option.toNullable item.published_time
            member _.Thumbnails = [
                match item.thumbs with
                | None -> ()
                | Some list -> for thumbnail in list do thumbnail.src
            ]
    }

    interface IDeviantArtClient with
        member _.GetGalleryFoldersAsync() = asyncSeq {
            let! token = asyncGetToken ()

            let folders =
                DeviantArtFs.Api.Gallery.GetFoldersAsync
                    token
                    (CalculateSize false)
                    (FolderPreload false)
                    (FilterEmptyFolder false)
                    ForCurrentUser
                    DefaultPagingLimit
                    StartingOffset

            for folder in folders do {
                new IFolder with
                    member _.FolderId = folder.folderid
                    member _.Name = folder.name
            }
        }

        member _.GetHomeFeedAsync() = asyncSeq {
            let! token = asyncGetToken ()

            let! page = Async.AwaitTask (DeviantArtFs.Api.Browse.PageHomeAsync token MaximumPagingLimit StartingOffset)

            for item in page.results |> Option.defaultValue [] do
                yield wrap item
        }

        member _.PostArtworkAsync(file, title, artistComments, tags, galleryFolders, isAI, disallowThirdPartyAITraining, _) = task {
            let! token = asyncGetToken ()

            let! response =
                Artwork.postArtworkAsync
                    token
                    file.Data
                    file.ContentType
                    title
                    artistComments
                    tags
                    galleryFolders
                    isAI
                    disallowThirdPartyAITraining

            if response.status <> "success" then
                raise (NotImplementedException $"DeviantArt response: {response.status}")

            let! deviation = DeviantArtFs.Api.Deviation.GetAsync token response.deviationid
            return wrap deviation
        }

        member _.PostJournalAsync(title, body, tags, _) = task {
            let! token = asyncGetToken ()
            let! response = Journal.postAsync token title body tags
            let! deviation = DeviantArtFs.Api.Deviation.GetAsync token response.deviationid
            return wrap deviation
        }

        member _.PostStatusAsync(message, _) = task {
            let! token = asyncGetToken ()
            let! response = Status.postAsync token message
            let! deviation = DeviantArtFs.Api.Deviation.GetAsync token response.statusid
            return wrap deviation
        }
