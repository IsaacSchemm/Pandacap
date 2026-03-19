namespace Pandacap.Weasyl.Models.WeasylApi

open System

type WhoamiResponse = {
    login: string
    userid: int
}

type AvatarResponse = {
    avatar: string
}

type Media = {
    url: string
}

type OwnerMedia = {
    avatar: Media list
}

type SubmissionMedia = {
    thumbnail: Media list
}

type Submission = {
    submitid: int
    title: string
    rating: string
    posted_at: DateTimeOffset
    ``type``: string
    owner: string
    owner_login: string
    owner_media: OwnerMedia option
    media: SubmissionMedia
    link: string
} with
    member this.Avatars =
        this.owner_media
        |> Option.toList
        |> List.collect (fun media -> media.avatar)

type SubmissionsResponse = {
    backtime: Nullable<int64>
    nexttime: Nullable<int64>
    submissions: Submission list
}

type MessagesSummary = {
    comments: int
    journals: int
    notifications: int
    submissions: int
    unread_notes: int
}
