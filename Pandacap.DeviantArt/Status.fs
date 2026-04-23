namespace Pandacap.DeviantArt

open FSharp.Control

open DeviantArtFs.Api.User

module internal Status =
    let postAsync token body = task {
        let content = {
            object = Nothing
            parent = NoParent
            stash_item = NoStashItem
        }

        return! PostStatusAsync token content body
    }
