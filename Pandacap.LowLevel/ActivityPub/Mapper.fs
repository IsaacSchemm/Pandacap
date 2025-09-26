namespace Pandacap.ActivityPub

open System

/// Provides mappings from Pandacap's internal IDs to the public ActivityPub IDs of corresponding objects.
type Mapper(appInfo: HostInformation) =
    member _.ActorId =
        $"https://{appInfo.ApplicationHostname}"

    member _.InboxId =
        $"https://{appInfo.ApplicationHostname}/ActivityPub/Inbox"

    member _.FollowersRootId =
        appInfo.ActivityPubFollowersRootId

    member _.FollowingRootId =
        $"https://{appInfo.ApplicationHostname}/ActivityPub/Following"

    member _.OutboxRootId =
        $"https://{appInfo.ApplicationHostname}/ActivityPub/Outbox"

    member _.FirstOutboxPageId =
        $"https://{appInfo.ApplicationHostname}/Gallery/Composite"

    member _.GetOutboxPageId(next: Guid, count: int) =
        $"https://{appInfo.ApplicationHostname}/Gallery/Composite?next={next}&count={count}"

    member _.LikedRootId =
        $"https://{appInfo.ApplicationHostname}/ActivityPub/Liked"

    member _.LikedPageId =
        $"https://{appInfo.ApplicationHostname}/Favorites"

    [<Obsolete("Use IPost.GetObjectId")>]
    member _.GetObjectId(myPost: IPost) =
        myPost.GetObjectId(appInfo)

    member _.GetCreateId(myPost: IPost) =
        $"{myPost.GetObjectId(appInfo)}/Created"

    member _.GetFollowId(followGuid: Guid) =
        $"https://{appInfo.ApplicationHostname}/ActivityPub/Follow/{followGuid}"

    member _.GetLikeId(likeId: Guid) =
        $"https://{appInfo.ApplicationHostname}/ActivityPub/Like/{likeId}"

    member _.GetTransientId() =
        $"https://{appInfo.ApplicationHostname}/ActivityPub/Transient/{Guid.NewGuid()}"
