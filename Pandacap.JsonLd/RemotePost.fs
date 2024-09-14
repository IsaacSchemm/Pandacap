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
    member this.ReplyTo = this.AttributedTo.Id
    member this.ReplyCc = List.distinct [
        for list in [this.To; this.Cc] do
            for addressee in list do
                match addressee with
                | Person a | Group a -> a.Id
                | _ -> ()
    ]
