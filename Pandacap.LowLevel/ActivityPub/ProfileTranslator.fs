namespace Pandacap.ActivityPub

open System
open System.Net

/// Creates ActivityPub objects (in string/object pair format) that represent the Pandacap actor.
type ProfileTranslator(hostInformation: HostInformation, mapper: Mapper) =
    let pair key value = (key, value :> obj)

    member _.BuildProfile(info: Profile) = dict [
        pair "id" mapper.ActorId
        pair "type" "Person"
        pair "inbox" mapper.InboxId
        pair "outbox" mapper.OutboxRootId
        pair "followers" mapper.FollowersRootId
        pair "following" mapper.FollowingRootId
        pair "liked" mapper.LikedRootId
        pair "preferredUsername" info.Username
        pair "name" info.Username
        pair "summary" (String.concat "" [
            $"<p>Art gallery hosted by <a href='{hostInformation.WebsiteUrl}'>{WebUtility.HtmlEncode(hostInformation.ApplicationName)}</a>.</p>"
        ])
        pair "url" mapper.ActorId
        pair "discoverable" true
        pair "indexable" true
        pair "publicKey" {|
            id = $"{mapper.ActorId}#main-key"
            owner = mapper.ActorId
            publicKeyPem = info.PublicKeyPem
        |}
        if not (isNull info.Avatar.Url) then
            pair "icon" {|
                mediaType = info.Avatar.MediaType
                ``type`` = "Image"
                url = info.Avatar.Url
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
        pair "id" (mapper.GetTransientId())
        pair "actor" mapper.ActorId
        pair "published" DateTimeOffset.UtcNow
        pair "object" (this.BuildProfile(info))
    ]
