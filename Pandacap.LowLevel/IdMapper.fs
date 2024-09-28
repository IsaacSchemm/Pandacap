namespace Pandacap.LowLevel

open System
open Pandacap.Data

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

    member _.GetOutboxPageId(position: ActivityPubPaginationPosition option) =
        match position with
        | Some pos ->
            $"https://{appInfo.ApplicationHostname}/Gallery/Composite?next={pos.next}&count={pos.count}"
        | None ->
            $"https://{appInfo.ApplicationHostname}/Gallery/Composite"

    member _.LikedRootId =
        $"https://{appInfo.ApplicationHostname}/ActivityPub/Liked"

    member _.LikedPageId =
        $"https://{appInfo.ApplicationHostname}/Favorites"

    member _.GetImageUrl(deviationid: Guid) =
        $"https://{appInfo.ApplicationHostname}/Blobs/Images/{deviationid}"

    member _.GetObjectId(userPost: UserPost) =
        $"https://{appInfo.ApplicationHostname}/UserPosts/{userPost.Id}"

    member _.GetObjectId(addressedPost: AddressedPost) =
        $"https://{appInfo.ApplicationHostname}/AddressedPosts/{addressedPost.Id}"

    member _.GetRepliesRootId(objectId: string) =
        $"https://{appInfo.ApplicationHostname}/RemoteReplies/Collection?objectId={Uri.EscapeDataString(objectId)}"

    member _.GetRepliesPageId(objectId: string, position: ActivityPubPaginationPosition option) =
        match position with
        | Some pos ->
            $"https://{appInfo.ApplicationHostname}/RemoteReplies/Page?objectId={Uri.EscapeDataString(objectId)}&next={pos.next}&count={pos.count}"
        | None ->
            $"https://{appInfo.ApplicationHostname}/RemoteReplies/Page?objectId={Uri.EscapeDataString(objectId)}"

    member _.GetFollowId(followGuid: Guid) =
        $"https://{appInfo.ApplicationHostname}/ActivityPub/Follow/{followGuid}"

    member _.GetLikeId(likeId: Guid) =
        $"https://{appInfo.ApplicationHostname}/ActivityPub/Like/{likeId}"

    member _.GetTransientId() =
        $"https://{appInfo.ApplicationHostname}/#transient-{Guid.NewGuid()}"
