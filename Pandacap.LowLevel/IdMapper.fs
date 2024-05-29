namespace Pandacap.LowLevel

/// Provides mappings between Pandacap's internal IDs and the public ActivityPub IDs of corresponding objects.
type IdMapper(appInfo: ApplicationInformation) =
    /// The ActivityPub actor ID of the single actor hosted by this Pandacap instance.
    member _.ActorId =
        $"https://{appInfo.ApplicationHostname}/api/actor"
