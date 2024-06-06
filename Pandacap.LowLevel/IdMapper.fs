namespace Pandacap.LowLevel

open System

/// Provides mappings between Pandacap's internal IDs and the public ActivityPub IDs of corresponding objects.
type IdMapper(appInfo: ApplicationInformation) =
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

    member _.GetImageUrl(deviationid: Guid) =
        $"https://{appInfo.ApplicationHostname}/Images/{deviationid}"

    member _.GetObjectId(deviationid: Guid) =
        $"https://{appInfo.ApplicationHostname}/BridgedPosts/{deviationid}"

    member _.GetFollowId(followGuid: Guid) =
        $"https://{appInfo.ApplicationHostname}/ActivityPub/Follow/{followGuid}"

    member _.GetTransientId() =
        $"https://{appInfo.ApplicationHostname}/#transient-{Guid.NewGuid()}"
