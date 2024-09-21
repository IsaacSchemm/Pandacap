namespace Pandacap.LowLevel

type Badge = {
    Text: string
    Background: string
    Color: string
}

type NotificationPlatform = {
    DisplayName: string
    Badge: Badge
    ViewAllUrl: string
} with
    static member ActivityPub = {
        DisplayName = "ActivityPub"
        Badge = {
            Text = "ActivityPub"
            Background = "#f1007e"
            Color = "white"
        }
        ViewAllUrl = null
    }

    static member ATProto = {
        DisplayName = "atproto"
        Badge = {
            Text = "atproto"
            Background = "#397EF6"
            Color = "white"
        }
        ViewAllUrl = "https://bsky.app/notifications"
    }

    static member DeviantArt = {
        DisplayName = "DeviantArt"
        Badge = {
            Text = "DeviantArt"
            Background = "#00e59b"
            Color = "black"
        }
        ViewAllUrl = "https://www.deviantart.com/notifications"
    }

    static member Weasyl = {
        DisplayName = "Weasyl"
        Badge = {
            Text = "Weasyl"
            Background = "#990000"
            Color = "white"
        }
        ViewAllUrl = "https://www.weasyl.com/messages/notifications"
    }
