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

            for did in info.Bluesky do
                $"<p>Bluesky: https://bsky.app/profile/{did}</p>"
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
            {|
                ``type`` = "PropertyValue"
                name = "ActivityPub"
                value = $"<a href='{mapper.ActorId}'>{WebUtility.HtmlEncode(mapper.ActorId)}</a>"
            |}
            for did in info.Bluesky do {|
                ``type`` = "PropertyValue"
                name = "Bluesky"
                value = $"<a href='https://bsky.app/profile/{did}'>{WebUtility.HtmlEncode(did)}</a>"
            |}
            {|
                ``type`` = "PropertyValue"
                name = "Twtxt"
                value = $"<a href='https://{hostInformation.ApplicationHostname}/Twtxt'>https://{WebUtility.HtmlEncode(hostInformation.ApplicationHostname)}/Twtxt</a>"
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
