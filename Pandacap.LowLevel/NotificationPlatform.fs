namespace Pandacap.LowLevel

type Badge = {
    Text: string
    Background: string
    Color: string
}

type NotificationPlatform =
| ActivityPubActivity
| ActivityPubPost
| ATProto
| DeviantArt
with
    member this.IsActivityPub =
        this = ActivityPubActivity || this = ActivityPubPost
    member this.Badge =
        match this with
        | ActivityPubActivity | ActivityPubPost ->
            { Text = "ActivityPub"
              Background = "#f1007e"
              Color = "white" }
        | ATProto ->
            { Text = "atproto"
              Background = "#397EF6"
              Color = "white" }
        | DeviantArt ->
            { Text = "DeviantArt"
              Background = "#00e59b"
              Color = "black" }
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
