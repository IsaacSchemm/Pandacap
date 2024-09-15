namespace Pandacap.JsonLd

open System

type RemotePost = {
    Id: string
    AttributedTo: RemoteActor
    To: Addressee list
    Cc: Addressee list
    InReplyTo: string list
    Type: string
    PostedAt: DateTimeOffset
    Sensitive: bool
    Name: string
    Summary: string
    SanitizedContent: string
    Url: string
    Audience: string
    Attachments: Attachment list
} with
    member this.Recipients = this.To @ this.Cc

    member this.People = [
        this.AttributedTo
        for r in this.Recipients do match r with Person a -> a | _ -> ()
    ]

    member this.Groups = [
        for r in this.Recipients do match r with Group a -> a | _ -> ()
    ]
