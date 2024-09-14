namespace Pandacap.JsonLd

open System

type RemotePost = {
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
    Attachments: Attachment list
}
