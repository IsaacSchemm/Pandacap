namespace Pandacap.PlatformLinks

open System
open FSharp.Control
open Pandacap.ActivityPub.Static
open Pandacap.Constants
open Pandacap.PlatformLinks.Interfaces
open Pandacap.PlatformLinks.ProfileInformation.Interfaces

module internal PlatformLinkProvider =
    type Platform = {
        category: PlatformLinkCategory
        name: string
        icon: string
        username: string option
        url: string option
        viewPost: IPlatformLinkPostSource -> string option
    } with
        interface IPlatformLink with
            member this.Category = this.category
            member this.IconFilename = this.icon
            member this.PlatformName = this.name
            member this.Text = Option.toObj this.username
            member this.Url = Option.toObj this.url

    let fediversePlatforms = [
        let handle = $"@{ActivityPubHostInformation.Username}@{ActivityPubHostInformation.ApplicationHostname}"

        let construct name icon = {
            category = PlatformLinkCategory.ActivityPub
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
            category = PlatformLinkCategory.ActivityPub
            name = "BrowserPub"
            icon = "browserpub.svg"
            username = Some handle
            url = Some $"https://browser.pub/{handle}"
            viewPost = fun post -> Some $"https://{post.ActivityPubObjectId}"
        }
    ]

    let getAllPlatforms (profile: ProfileInformation) = seq {
        yield! fediversePlatforms

        for handle in profile.BlueskyHandles do
            for appView in BlueskyAppViews.All do {
                category = PlatformLinkCategory.Bluesky
                name = appView.name
                icon = appView.icon
                username = Some handle
                url = Some $"https://{appView.host}/profile/{handle}"
                viewPost = fun post ->
                    if isNull post.BlueskyDID || isNull post.BlueskyRecordKey
                    then None
                    else Some $"https://{appView.host}/profile/{post.BlueskyDID}/post/{post.BlueskyRecordKey}"
            }

        for username in profile.DeviantArtUsernames do {
            category = PlatformLinkCategory.DeviantArt
            name = "DeviantArt"
            icon = "deviantart.png"
            username = Some username
            url = Some $"https://www.deviantart.com/{Uri.EscapeDataString(username)}"
            viewPost = fun post -> Option.ofObj post.DeviantArtUrl
        }

        for username in profile.FurAffinityUsernames do {
            category = PlatformLinkCategory.FurAffinity
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
            category = PlatformLinkCategory.Weasyl
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
        url: string option
    } with
        interface IPlatformLink with
            member this.Category = this.platform.category
            member this.IconFilename = this.platform.icon
            member this.PlatformName = this.platform.name
            member _.Text = null
            member this.Url = Option.toObj this.url

    let getAllPostLinks profile post = seq {
        for platform in getAllPlatforms profile do {
            platform = platform
            url = platform.viewPost post
        }
    }

type PlatformLinkProvider(
    profileInformationProvider: IProfileInformationProvider
) =
    let shouldDisplay (platformLink: IPlatformLink) =
        not (isNull platformLink.Url) || not (isNull platformLink.Text)

    let asyncGetProfile = async {
        let! token = Async.CancellationToken
        return! Async.AwaitTask(
            profileInformationProvider.GetProfileInformationAsync(
                token))
    }

    interface IPlatformLinkProvider with
        member _.GetPostLinksAsync(post) = asyncSeq {
            let! profile = asyncGetProfile
            for link in PlatformLinkProvider.getAllPostLinks profile post do
                if shouldDisplay link then
                    yield link :> IPlatformLink
        }

        member _.GetProfileLinksAsync() = asyncSeq {
            let! profile = asyncGetProfile
            for link in PlatformLinkProvider.getAllPlatforms profile do
                if shouldDisplay link then
                    yield link :> IPlatformLink
        }
