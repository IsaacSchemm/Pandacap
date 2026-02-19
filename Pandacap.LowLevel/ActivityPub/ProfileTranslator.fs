namespace Pandacap.ActivityPub

open System
open System.Net

/// Creates ActivityPub objects (in string/object pair format) that represent the Pandacap actor.
type ProfileTranslator(hostInformation: HostInformation) =
    let pair key value = (key, value :> obj)

    member _.BuildProfile(info: Profile) = dict [
        pair "id" hostInformation.ActorId
        pair "type" "Person"
        pair "inbox" $"https://{hostInformation.ApplicationHostname}/ActivityPub/Inbox"
        pair "outbox" $"https://{hostInformation.ApplicationHostname}/ActivityPub/Outbox"
        pair "followers" $"https://{hostInformation.ApplicationHostname}/ActivityPub/Followers"
        pair "following" $"https://{hostInformation.ApplicationHostname}/ActivityPub/Following"
        pair "liked" $"https://{hostInformation.ApplicationHostname}/ActivityPub/Liked"
        pair "preferredUsername" info.Username
        pair "name" info.Username
        pair "summary" (String.concat "" [
            $"<p>Hosted by <a href='{hostInformation.WebsiteUrl}'>{WebUtility.HtmlEncode(hostInformation.ApplicationName)}</a>.</p>"
        ])
        pair "url" hostInformation.ActorId
        pair "discoverable" true
        pair "indexable" true
        pair "publicKey" {|
            id = $"{hostInformation.ActorId}#main-key"
            owner = hostInformation.ActorId
            publicKeyPem = info.PublicKeyPem
        |}
        for avatar in info.Avatars do
            pair "icon" {|
                mediaType = avatar.MediaType
                ``type`` = "Image"
                url = avatar.Url
            |}
        pair "attachment" [
            for link in info.Links do
                if link.platformName <> "ActivityPub" then {|
                    ``type`` = "PropertyValue"
                    name = WebUtility.HtmlEncode(link.platformName)
                    value = $"<a href='{link.url}'>{WebUtility.HtmlEncode(link.linkText)}</a>"
                |}
        ]
    ]

    member this.BuildProfileUpdate(info) = dict [
        pair "type" "Update"
        pair "id" (hostInformation.GenerateTransientObjectId())
        pair "actor" hostInformation.ActorId
        pair "published" DateTimeOffset.UtcNow
        pair "object" (this.BuildProfile(info))
    ]
