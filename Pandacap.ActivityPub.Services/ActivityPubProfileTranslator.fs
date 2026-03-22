namespace Pandacap.ActivityPub.Services

open System
open System.Net
open Pandacap.ActivityPub.Static
open Pandacap.ActivityPub.Models
open Pandacap.ActivityPub.Services.Interfaces

/// Creates ActivityPub objects (in string/object pair format) that represent the Pandacap actor.
type ActivityPubProfileTranslator() =
    let pair key value = (key, value :> obj)

    member _.BuildProfile(info: ActivityPubProfile) = dict [
        let actorId = ActivityPubHostInformation.GetActorId()

        pair "id" actorId
        pair "type" "Person"
        pair "inbox" $"https://{ActivityPubHostInformation.ApplicationHostname}/ActivityPub/Inbox"
        pair "outbox" $"https://{ActivityPubHostInformation.ApplicationHostname}/ActivityPub/Outbox"
        pair "followers" $"https://{ActivityPubHostInformation.ApplicationHostname}/ActivityPub/Followers"
        pair "following" $"https://{ActivityPubHostInformation.ApplicationHostname}/ActivityPub/Following"
        pair "liked" $"https://{ActivityPubHostInformation.ApplicationHostname}/ActivityPub/Liked"
        pair "preferredUsername" info.Username
        pair "name" info.Username
        pair "summary" info.SummaryHtml
        pair "url" actorId
        pair "discoverable" true
        pair "indexable" true
        pair "publicKey" {|
            id = $"{actorId}#main-key"
            owner = actorId
            publicKeyPem = info.PublicKeyPem
        |}
        for avatar in info.Avatars do
            pair "icon" {|
                mediaType = avatar.MediaType
                ``type`` = "Image"
                url = avatar.Url
            |}
        pair "attachment" [
            for link in info.Links do {|
                ``type`` = "PropertyValue"
                name = WebUtility.HtmlEncode(link.PlatformName)
                value = $"<a href='{link.ViewProfileUrl}'>{WebUtility.HtmlEncode(link.Username)}</a>"
            |}
        ]
    ]

    member this.BuildProfileUpdate(info) = dict [
        pair "type" "Update"
        pair "id" (ActivityPubHostInformation.GenerateTransientObjectId())
        pair "actor" (ActivityPubHostInformation.GetActorId())
        pair "published" DateTimeOffset.UtcNow
        pair "object" (this.BuildProfile(info))
    ]

    interface IActivityPubProfileTranslator with
        member this.BuildProfile(profile) =
            profile
            |> this.BuildProfile
            |> ActivityPubSerializer.SerializeWithContext

        member this.BuildProfileUpdate(profile) =
            profile
            |> this.BuildProfileUpdate
            |> ActivityPubSerializer.SerializeWithContext
