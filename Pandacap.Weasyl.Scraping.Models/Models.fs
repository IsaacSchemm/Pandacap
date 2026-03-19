namespace Pandacap.Weasyl.Scraping.Models

open System

type SubmissionsPage = {
    submitids: int list
    nextid: Nullable<int>
}

type NotificationLink = {
    href: string
    name: string
}

type NotificationGroup = {
    users: NotificationLink option
    time: DateTimeOffset
    posts: NotificationLink option
}

type ExtractedNotification = {
    Id: string
    PostUrl: string
    Time: DateTimeOffset
    UserName: string
    UserUrl: string
}

type ExtractedJournal = {
    time: DateTimeOffset
    user: NotificationLink
    post: NotificationLink
}

type ExtractedNote = {
    title: string
    sender: string
    sender_url: string
    time: DateTimeOffset
}
