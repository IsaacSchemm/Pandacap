namespace Pandacap.LowLevel

open System

/// Provides mappings between Pandacap's internal IDs and the public ActivityPub IDs of corresponding objects.
type IdMapper(appInfo: ApplicationInformation) =
    /// The ActivityPub actor ID of the single actor hosted by this Pandacap instance.
    member _.ActorId =
        $"https://{appInfo.ApplicationHostname}"

    member _.InboxId =
        $"https://{appInfo.ApplicationHostname}/ActivityPub/Inbox"

    member _.OutboxId =
        $"https://{appInfo.ApplicationHostname}/ActivityPub/Outbox"

    member _.FollowersRootId =
        $"https://{appInfo.ApplicationHostname}/ActivityPub/Followers"

    member _.FollowersPageId =
        $"https://{appInfo.ApplicationHostname}/Profile/Followers"

    member _.FollowingRootId =
        $"https://{appInfo.ApplicationHostname}/ActivityPub/Following"

    member _.FollowingPageId =
        $"https://{appInfo.ApplicationHostname}/Profile/Following"

    /// Gets a URL that can be used to retrieve the original image (e.g. PNG,
    /// JPEG), proxied through Pandacap.
    member _.GetImageUrl(deviationid: Guid) =
        $"https://{appInfo.ApplicationHostname}/Images/{deviationid}"

    /// Determines the ActivityPub object ID for a post.
    member _.GetObjectId(deviationid: Guid) =
        $"https://{appInfo.ApplicationHostname}/Post/{deviationid}"

    /// Determines the ActivityPub object ID for an activity.
    member _.GetActivityId(activityGuid: Guid) =
        $"https://{appInfo.ApplicationHostname}/ActivityPub/Activities/{activityGuid}"
