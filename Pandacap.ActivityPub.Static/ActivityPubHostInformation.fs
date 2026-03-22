namespace Pandacap.ActivityPub.Static

open System

module ActivityPubHostInformation =
    let mutable ApplicationHostname = ""

    let GetActorId() =
        $"https://{ApplicationHostname}"

    let GenerateTransientObjectId() =
        $"https://{ApplicationHostname}/ActivityPub/Transient/{Guid.NewGuid()}"
