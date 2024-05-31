namespace Pandacap.LowLevel

open System

/// Provides mappings between Pandacap's internal IDs and the public ActivityPub IDs of corresponding objects.
type IdMapper(appInfo: ApplicationInformation) =
    /// The ActivityPub actor ID of the single actor hosted by this Pandacap instance.
    member _.ActorId =
        $"https://{appInfo.ApplicationHostname}/ActivityPub/Actor"

    member _.InboxId =
        $"https://{appInfo.ApplicationHostname}/ActivityPub/Inbox"

    member _.OutboxId =
        $"https://{appInfo.ApplicationHostname}/ActivityPub/Outbox"

    /// Determines the ActivityPub object ID for a post.
    member _.GetObjectId(deviationid: Guid) =
        $"https://{appInfo.ApplicationHostname}/Posts/{deviationid}"

    /// Determines the ActivityPub object ID for an activity.
    member _.GetActivityId(activityGuid: Guid) =
        $"https://{appInfo.ApplicationHostname}/ActivityPub/Activities/{activityGuid}"
