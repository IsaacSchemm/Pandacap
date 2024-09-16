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
    member this.Recipients = this.To @ this.Cc

    //member this.Actors = seq {
    //    yield this.AttributedTo

    //    for r in this.Recipients do
    //        match r with
    //        | Actor actor -> yield actor
    //        | _ -> ()
    //}

    //member this.Groups =
    //    this.Actors
    //    |> Seq.where (fun a -> a.Type = "https://www.w3.org/ns/activitystreams#Group")

    //member this.People =
    //    this.Actors
    //    |> Seq.except this.Groups
