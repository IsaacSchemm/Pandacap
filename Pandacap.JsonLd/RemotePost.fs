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
    Attachments: Attachment list
} with
    member this.ExplicitAddressees = [
        for a in this.To @ this.Cc do
            match a with
            | Person x | Group x -> x
            | _ -> ()
    ]
