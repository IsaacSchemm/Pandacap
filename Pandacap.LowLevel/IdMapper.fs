namespace Pandacap.LowLevel

open System

/// Provides mappings between Pandacap's internal IDs and the public ActivityPub IDs of corresponding objects.
type IdMapper(appInfo: ApplicationInformation) =
    member _.ActorId =
        $"https://{appInfo.ApplicationHostname}"

    member _.AvatarUrl =
        $"https://{appInfo.ApplicationHostname}/Blobs/Avatar"

    member _.InboxId =
        $"https://{appInfo.ApplicationHostname}/ActivityPub/Inbox"

    member _.FollowersRootId =
        $"https://{appInfo.ApplicationHostname}/ActivityPub/Followers"

    member _.FollowersPageId =
        $"https://{appInfo.ApplicationHostname}/Profile/Followers"

    member _.FollowingRootId =
        $"https://{appInfo.ApplicationHostname}/ActivityPub/Following"

    member _.FollowingPageId =
        $"https://{appInfo.ApplicationHostname}/Profile/Following"

    member _.OutboxRootId =
        $"https://{appInfo.ApplicationHostname}/ActivityPub/Outbox"

    member _.OutboxPageId =
        $"https://{appInfo.ApplicationHostname}/Gallery/Composite"

    member _.LikedRootId =
        $"https://{appInfo.ApplicationHostname}/ActivityPub/Liked"

    member _.LikedPageId =
        $"https://{appInfo.ApplicationHostname}/Favorites"

    member _.GetImageUrl(deviationid: Guid) =
        $"https://{appInfo.ApplicationHostname}/Blobs/Images/{deviationid}"

    member _.GetObjectId(deviationid: Guid) =
        $"https://{appInfo.ApplicationHostname}/UserPosts/{deviationid}"

    member _.GetFollowId(followGuid: Guid) =
        $"https://{appInfo.ApplicationHostname}/ActivityPub/Follow/{followGuid}"

    member _.GetLikeId(likeId: Guid) =
        $"https://{appInfo.ApplicationHostname}/ActivityPub/Like/{likeId}"

    member _.GetTransientId() =
        $"https://{appInfo.ApplicationHostname}/#transient-{Guid.NewGuid()}"
