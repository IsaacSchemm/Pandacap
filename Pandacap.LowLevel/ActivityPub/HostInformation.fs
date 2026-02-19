namespace Pandacap.ActivityPub

open System

/// Information about the Pandacap deployment, in an ActivityPub context.
type HostInformation = {
    ApplicationHostname: string
    ApplicationName: string
    WebsiteUrl: string
} with
    member this.ActorId =
        $"https://{this.ApplicationHostname}"

    member this.GenerateTransientObjectId() =
        $"https://{this.ApplicationHostname}/ActivityPub/Transient/{Guid.NewGuid()}"
