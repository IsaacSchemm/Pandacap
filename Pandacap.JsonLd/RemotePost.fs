namespace Pandacap.JsonLd

open System

type RemotePost = {
    Id: string
    AttributedTo: RemoteActor
    To: RemoteAddressee list
    Cc: RemoteAddressee list
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
    member this.HasVisibleSummary = not (String.IsNullOrWhiteSpace(this.Summary))
    member this.Recipients = this.To @ this.Cc
