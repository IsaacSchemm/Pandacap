namespace Pandacap.ActivityPub.Models

open System
open Pandacap.ActivityPub.Models.Interfaces

/// Information about the Pandacap deployment, in an ActivityPub context.
type ActivityPubHostInformation = {
    ApplicationHostname: string
    ApplicationName: string
    WebsiteUrl: string
} with
    member this.ActorId =
        $"https://{this.ApplicationHostname}"

    member this.GenerateTransientObjectId() =
        $"https://{this.ApplicationHostname}/ActivityPub/Transient/{Guid.NewGuid()}"

    member this.BaseUri =
        new Uri($"https://{this.ApplicationHostname}")

    member this.GetAbsoluteUri(r: IActivityPubPandacapRelativePath) =
        (new Uri(this.BaseUri, r.RelativePath)).AbsoluteUri

    member this.GetObjectId(post: IActivityPubPost) =
        this.GetAbsoluteUri(post.ObjectId)

    member this.GetAddressing(post: IActivityPubPost) = {|
        InReplyTo = post.Addressing.InReplyTo
        To = [for x in post.Addressing.To do this.GetAbsoluteUri(x)]
        Cc = [for x in post.Addressing.Cc do this.GetAbsoluteUri(x)]
        Audience = post.Addressing.Audience
    |}
