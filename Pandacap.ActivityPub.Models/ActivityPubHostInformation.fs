namespace Pandacap.ActivityPub.Models

open System
open Pandacap.Configuration

module ActivityPubHostInformation =
    let ApplicationHostname = DeploymentInformation.ApplicationHostname
    let Username = DeploymentInformation.Username
    let ActorId = $"https://{ApplicationHostname}"

    let GenerateTransientObjectId() =
        $"https://{ApplicationHostname}/ActivityPub/Transient/{Guid.NewGuid()}"
