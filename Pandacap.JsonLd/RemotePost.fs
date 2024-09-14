namespace Pandacap.JsonLd

open System

type RemotePost = {
    Id: string
    AttributedTo: Addressee
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
}
