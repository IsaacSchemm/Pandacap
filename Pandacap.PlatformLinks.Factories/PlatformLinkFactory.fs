namespace Pandacap.PlatformLinks.Factories

open System
open Pandacap.Constants
open Pandacap.PlatformLinks.Interfaces

module PlatformLinkFactory =
    type Platform = {
        category: string
        name: string
        icon: string
        username: string option
        url: string option
        viewPost: IPlatformLinkPost -> string option
    } with
        interface IPlatformLink with
            member this.Category = this.category
            member this.IconFilename = this.icon
            member this.PlatformName = this.name
            member this.Text = Option.toObj this.username
            member this.Url = Option.toObj this.url

    let GetAllPlatforms(profile: IPlatformLinkProfile) = seq {
        for handle in profile.ActivityPubWebFingerHandles do
            let construct name icon = {
                category = "ActivityPub"
                name = name
                icon = icon
                username = Some handle
                url = None
                viewPost = fun _ -> None
            }

            construct "ActivityPub" "activitypub.svg"
            construct "Mastodon" "mastodon.png"
            construct "Pixelfed" "pixelfed.png"
            construct "wafrn" "wafrn.png"

            {
                category = "ActivityPub"
                name = "BrowserPub"
                icon = "browserpub.svg"
                username = Some handle
                url = Some $"https://browser.pub/{handle}"
                viewPost = fun post -> Some $"https://browser.pub/{post.ActivityPubObjectId}"
            }

        for handle in profile.BlueskyHandles do
            for appView in BlueskyAppViews.All do {
                category = "Bluesky"
                name = appView.name
                icon = appView.icon
                username = Some $"@{handle}"
                url = Some $"https://{appView.host}/profile/{handle}"
                viewPost = fun post ->
                    if isNull post.BlueskyDID || isNull post.BlueskyRecordKey
                    then None
                    else Some $"https://{appView.host}/profile/{post.BlueskyDID}/post/{post.BlueskyRecordKey}"
            }

        for username in profile.DeviantArtUsernames do {
            category = "DeviantArt"
            name = "DeviantArt"
            icon = "deviantart.png"
            username = Some username
            url = Some $"https://www.deviantart.com/{Uri.EscapeDataString(username)}"
            viewPost = fun post -> Option.ofObj post.DeviantArtUrl
        }

        for username in profile.FurAffinityUsernames do {
            category = "Fur Affinity"
            name = "Fur Affinity"
            icon = "furaffinity.ico"
            username = Some username
            url = Some $"https://www.furaffinity.net/user/{Uri.EscapeDataString(username)}/"
            viewPost = fun post ->
                if post.FurAffinitySubmissionId.HasValue then Some $"https://www.furaffinity.net/view/{post.FurAffinitySubmissionId}/"
                else if post.FurAffinityJournalId.HasValue then Some $"https://www.furaffinity.net/journal/{post.FurAffinitySubmissionId}/"
                else None
        }

        for username in profile.WeasylUsernames do {
            category = "Weasyl"
            name = "Weasyl"
            icon = "weasyl.svg"
            username = Some username
            url = Some $"https://www.weasyl.com/~{Uri.EscapeDataString(username)}"
            viewPost = fun post ->
                if post.WeasylSubmitId.HasValue then Some $"https://www.weasyl.com/~{Uri.EscapeDataString(username)}/submissions/{post.WeasylSubmitId}"
                else if post.WeasylJournalId.HasValue then Some $"https://www.weasyl.com/journal/{post.WeasylJournalId}/"
                else None
        }
    }

    type PlatformPost = {
        platform: Platform
        url: string
    } with
        interface IPlatformLink with
            member this.Category = this.platform.category
            member this.IconFilename = this.platform.icon
            member this.PlatformName = this.platform.name
            member _.Text = null
            member this.Url = this.url

    let GetAllPostLinks(profile, post) = seq {
        for platform in GetAllPlatforms profile do
            match platform.viewPost post with
            | None -> ()
            | Some url ->
                yield {
                    platform = platform
                    url = url
                }
    }
