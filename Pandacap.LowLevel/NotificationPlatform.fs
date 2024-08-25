namespace Pandacap.LowLevel

open System

type NotificationPlatform =
| ActivityPubActivity
| ActivityPubPost
| ATProto
| DeviantArt
with
    member this.DisplayName =
        match this with
        | ActivityPubActivity -> "ActivityPub (activities)"
        | ActivityPubPost -> "ActivityPub (posts)"
        | ATProto -> "atproto"
        | DeviantArt -> "DeviantArt"
    member this.ViewAllUrl =
        match this with
        | ActivityPubActivity -> null
        | ActivityPubPost -> "/Inbox/ActivityPubMentionsAndReplies"
        | ATProto -> "https://bsky.app/notifications"
        | DeviantArt -> "https://www.deviantart.com/notifications/"
