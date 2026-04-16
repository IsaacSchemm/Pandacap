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
        icon: string option
        username: string option
        url: string option
        viewPost: IPlatformLinkPostSource -> string option
    } with
        interface IPlatformLink with
            member this.Category = this.category
            member this.IconFilename = Option.toObj this.icon
            member this.PlatformName = this.name
            member this.Text = Option.toObj this.username
            member this.Url = Option.toObj this.url

    let fediversePlatforms = [
        let handle = $"@{ActivityPubHostInformation.Username}@{ActivityPubHostInformation.ApplicationHostname}"

        let construct name icon = {
            category = PlatformLinkCategory.ActivityPub
            name = name
            icon = match icon with "" -> None | x -> Some x
            username = Some handle
            url = None
            viewPost = fun _ -> None
        }

        construct "ActivityPub" ""
        construct "Mastodon"    "mastodon.png"
        construct "Pixelfed"    "pixelfed.png"
        construct "wafrn"       "wafrn.png"

        {
            category = PlatformLinkCategory.ActivityPub
            name = "BrowserPub"
            icon = Some "browserpub.svg"
            username = Some handle
            url = Some $"https://browser.pub/{handle}"
            viewPost = fun post -> Some $"https://{post.ActivityPubObjectId}"
        }
    ]

    let getBlueskyAppViews (profile: PlatformLinkProfileInformation) = seq {
        for account in profile.BlueskyAccounts do
            for appView in BlueskyAppViews.All do {
                category = PlatformLinkCategory.Bluesky
                name = appView.name
                icon = Some appView.icon
                username =
                    if isNull account.Handle
                    then None
                    else Some $"@{account.Handle}"
                url = Some $"https://{appView.host}/profile/{account.DID}"
                viewPost = fun post ->
                    match post.BlueskyDID, post.BlueskyRecordKey with
                    | null, _
                    | _, null -> None
                    | did, rkey -> Some $"https://{appView.host}/profile/{did}/post/{rkey}"
            }
    }

    let getCrosspostTargets (profile: PlatformLinkProfileInformation) = seq {
        for username in profile.DeviantArtUsernames do {
            category = PlatformLinkCategory.DeviantArt
            name = "DeviantArt"
            icon = Some "deviantart.png"
            username = Some username
            url = Some $"https://www.deviantart.com/{Uri.EscapeDataString(username)}"
            viewPost = fun post -> Option.ofObj post.DeviantArtUrl
        }

        for username in profile.FurAffinityUsernames do {
            category = PlatformLinkCategory.FurAffinity
            name = "Fur Affinity"
            icon = Some "furaffinity.ico"
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
            icon = Some "weasyl.svg"
            username = Some username
            url = Some $"https://www.weasyl.com/~{Uri.EscapeDataString(username)}"
            viewPost = fun post ->
                if post.WeasylSubmitId.HasValue then Some $"https://www.weasyl.com/~{Uri.EscapeDataString(username)}/submissions/{post.WeasylSubmitId}"
                else if post.WeasylJournalId.HasValue then Some $"https://www.weasyl.com/journal/{post.WeasylJournalId}/"
                else None
        }
    }

    let getAllPlatforms (profile: PlatformLinkProfileInformation) = seq {
        yield! fediversePlatforms
        yield! getBlueskyAppViews profile
        yield! getCrosspostTargets profile
    }

    type PlatformPost = {
        platform: Platform
        url: string option
    } with
        interface IPlatformLink with
            member this.Category = this.platform.category
            member this.IconFilename = Option.toObj this.platform.icon
            member this.PlatformName = this.platform.name
            member _.Text = null
            member this.Url = Option.toObj this.url

    let getAllPostLinks (profile: PlatformLinkProfileInformation) (post: IPlatformLinkPostSource) = seq {
        for platform in getAllPlatforms profile do {
            platform = platform
            url = platform.viewPost post
        }
    }

    let linkHasUsefulInformation (platformLink: IPlatformLink) =
        not (isNull platformLink.Url) || not (isNull platformLink.Text)

type PlatformLinkProvider(
    platformLinkProfileInformationProvider: IPlatformLinkProfileInformationProvider
) =
    interface IPlatformLinkProvider with
        member _.GetPostLinksAsync(post) = asyncSeq {
            let! token = Async.CancellationToken
            let! profile =
                platformLinkProfileInformationProvider.GetProfileInformationAsync(token)
                |> Async.AwaitTask
            for link in PlatformLinkProvider.getAllPostLinks profile post do
                if PlatformLinkProvider.linkHasUsefulInformation link then
                    yield link :> IPlatformLink
        }

        member _.GetProfileLinksAsync() = asyncSeq {
            let! token = Async.CancellationToken
            let! profile =
                platformLinkProfileInformationProvider.GetProfileInformationAsync(token)
                |> Async.AwaitTask
            for link in PlatformLinkProvider.getAllPlatforms profile do
                if PlatformLinkProvider.linkHasUsefulInformation link then
                    yield link :> IPlatformLink
        }
