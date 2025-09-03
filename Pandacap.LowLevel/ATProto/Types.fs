namespace Pandacap.Clients.ATProto

open System

type ATProtoTokens = {
    AccessToken: string
    RefreshToken: string
    Handle: string
    DID: string
}

type ATProtoRefUri = {
    Raw: string
} with
    override this.ToString() =
        this.Raw

    member this.Components =
        let split =
            this.Raw
            |> Option.ofObj
            |> Option.filter (fun uri -> uri.StartsWith("at://"))
            |> Option.map (fun str -> str.Split('/'))
            |> Option.defaultValue [||]

        {|
            DID = split |> Seq.tryItem 2 |> Option.toObj
            Collection = split |> Seq.tryItem 3 |> Option.toObj
            RecordKey = split |> Seq.tryItem 4 |> Option.toObj
        |}

type ATProtoRef = {
    CID: string
    Uri: ATProtoRefUri
}

type ATProtoRecord<'T> = {
    Ref: ATProtoRef
    Value: 'T
}

type ATProtoPage<'T> = {
    Items: 'T list
    Cursor: string
}

type ATProtoListDirection =
| Forward
| Reverse

type BlueskyProfile = {
    AvatarCID: string
    DisplayName: string
    Description: string
}

type BlueskyImage = {
    CID: string
    Alt: string
}

type BlueskyReplyContext = {
    Parent: ATProtoRef
    Root: ATProtoRef
}

type BlueskyPost = {
    Text: string
    Images: BlueskyImage list
    Quoted: ATProtoRef list
    InReplyTo: BlueskyReplyContext list
    BridgyOriginalUrl: string
    Labels: string list
    CreatedAt: DateTimeOffset
}

type BlueskyInteraction = {
    CreatedAt: DateTimeOffset
    Subject: ATProtoRef
}

type BlueskyAppViewNotificationAuthor = {
    DID: string
    Handle: string
    DisplayName: string
}

type BlueskyAppViewNotification = {
    Ref: ATProtoRef
    Author: BlueskyAppViewNotificationAuthor
    Reason: string
    ReasonSubject: ATProtoRefUri
    IsRead: bool
    IndexedAt: DateTimeOffset
}

type WhitewindBlogEntry = {
    Title: string
    Content: string
    CreatedAt: Nullable<DateTimeOffset>
    Public: bool
}

type BlueskyEmbeddedImageParameters = {
    Blob: obj
    Alt: string
    Width: int
    Height: int
}

type BlueskyPostParameters = {
    Text: string
    CreatedAt: DateTimeOffset
    Images: BlueskyEmbeddedImageParameters list
    InReplyTo: BlueskyReplyContext list
    PandacapPost: Nullable<Guid>
}

type ATProtoCreateParameters =
| BlueskyPost of BlueskyPostParameters
| BlueskyLike of ATProtoRef
