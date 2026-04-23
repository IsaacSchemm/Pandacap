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

    let wrapUser (author: DeviantArtFs.ResponseTypes.User) = {
        new IAuthor with
            member _.UserId = author.userid
            member _.UserIcon = author.usericon
            member _.Username = author.username
    }

    let wrap (item: DeviantArtFs.ResponseTypes.Deviation) = {
        new IDeviation with
            member _.Title = Option.toObj item.title
            member _.DeviationId = item.deviationid
            member _.Url = Option.toObj item.url
            member _.PublishedTime = Option.toNullable item.published_time
            member _.Thumbnails =
                item.thumbs
                |> Option.defaultValue []
                |> Seq.sortByDescending (fun t -> t.width * t.height)
                |> Seq.map (fun t -> t.src)
            member _.Author =
                item.author
                |> Option.map wrapUser
                |> Option.toObj
            member _.Excerpt = Option.toObj item.excerpt
            member _.IsMature = item.is_mature = Some true
    }

    interface IDeviantArtClient with
        member _.GetByUsersYouWatchAsync() = asyncSeq {
            let! token = asyncGetToken ()

            for deviation in DeviantArtFs.Api.Browse.GetByDeviantsYouWatchAsync token DefaultPagingLimit StartingOffset do
                wrap deviation
        }

        member _.GetFavoritesAsync() = asyncSeq {
            let! token = asyncGetToken ()

            for deviation in DeviantArtFs.Api.Collections.GetAllAsync token UserScope.ForCurrentUser DefaultPagingLimit StartingOffset do
                wrap deviation
        }

        member _.GetFriendsAsync() = asyncSeq {
            let! token = asyncGetToken ()

            for user in DeviantArtFs.Api.User.GetFriendsAsync token ForCurrentUser DefaultPagingLimit StartingOffset do {
                new IRelationship with
                    member _.AreYouWatching = user.is_watching
                    member _.IsWatchingYou = user.watches_you
                    member _.LastVisit = Option.toNullable user.lastvisit
                    member _.Username = user.user.username
            }
        }

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

        member _.GetMessagesInInboxAsync() = asyncSeq {
            let! token = asyncGetToken ()

            for message in Messages.getInboxFeedAsync token do {
                new IMessage with
                    member _.Deviation =
                        message.subject
                        |> Option.bind (fun s -> s.deviation)
                        |> Option.map wrap
                        |> Option.toObj
                    member _.From =
                        message.originator
                        |> Option.map wrapUser
                        |> Option.toObj
                    member _.Timestamp = Option.toNullable message.ts
                    member _.Type = message.``type``
            }
        }

        member _.GetNotesInInboxAsync() = asyncSeq {
            let! token = asyncGetToken ()

            for note in Notes.getInboxAsync token do {
                new INote with
                    member _.From = wrapUser note.user
                    member _.Timestamp = note.ts
            }
        }

        member _.GetProfilePostsAsync(username) = asyncSeq {
            let! token = asyncGetToken ()

            for deviation in DeviantArtFs.Api.User.GetProfilePostsAsync token username DeviantArtFs.Api.User.FromBeginning do
                wrap deviation
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
