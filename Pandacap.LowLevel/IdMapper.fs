namespace Pandacap.LowLevel

open System

/// Provides mappings between Pandacap's internal IDs and the public ActivityPub IDs of corresponding objects.
type IdMapper(appInfo: ApplicationInformation) =
    /// The ActivityPub actor ID of the single actor hosted by this Pandacap instance.
    member _.ActorId =
        $"https://{appInfo.ApplicationHostname}/ap/actor"

    /// Generates a random ID that is not intended to be looked up.
    /// Used for Update and Delete activities.
    member _.GenerateTransientId() =
        $"https://{appInfo.ApplicationHostname}#transient-{Guid.NewGuid()}"

    /// Determines the ActivityPub object ID for a post.
    member _.GetObjectId(deviationid: Guid) =
        $"https://{appInfo.ApplicationHostname}/Posts/{deviationid}"

    /// Determines the ActivityPub object ID for a Create activity.
    member _.GetCreateId(deviationid: Guid) =
        $"https://{appInfo.ApplicationHostname}/ap/create/{deviationid}"
