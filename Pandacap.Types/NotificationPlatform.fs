namespace Pandacap.Types

type NotificationPlatform = {
    DisplayName: string
    Badge: Badge
    ViewAllUrl: string
} with
    static member ActivityPub = {
        DisplayName = "ActivityPub"
        Badge = PostPlatform.GetBadge ActivityPub
        ViewAllUrl = null
    }

    static member ATProto = {
        DisplayName = "atproto"
        Badge = PostPlatform.GetBadge ATProto
        ViewAllUrl = "https://bsky.app/notifications"
    }

    static member DeviantArt = {
        DisplayName = "DeviantArt"
        Badge = PostPlatform.GetBadge DeviantArt
        ViewAllUrl = "https://www.deviantart.com/notifications"
    }

    static member Weasyl = {
        DisplayName = "Weasyl"
        Badge = PostPlatform.GetBadge Weasyl
        ViewAllUrl = "https://www.weasyl.com/messages/notifications"
    }
