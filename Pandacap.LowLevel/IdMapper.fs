namespace Pandacap.LowLevel

open System
open Pandacap.Data

/// Provides mappings between Pandacap's internal IDs and the public ActivityPub IDs of corresponding objects.
type IdMapper(appInfo: ApplicationInformation) =
    member _.ActorId =
        $"https://{appInfo.ApplicationHostname}"

    member _.GetAvatarUrl(avatar: Avatar) =
        $"https://{appInfo.ApplicationHostname}/Blobs/Avatar/{avatar.Id}"

    member _.InboxId =
        $"https://{appInfo.ApplicationHostname}/ActivityPub/Inbox"

    member _.FollowersRootId =
        $"https://{appInfo.ApplicationHostname}/ActivityPub/Followers"

    member _.FollowingRootId =
        $"https://{appInfo.ApplicationHostname}/ActivityPub/Following"

    member _.OutboxRootId =
        $"https://{appInfo.ApplicationHostname}/ActivityPub/Outbox"

    member _.FirstOutboxPageId =
        $"https://{appInfo.ApplicationHostname}/Gallery/Composite"

    member this.GetOutboxPageId(next: Guid, count: int) =
        $"{this.FirstOutboxPageId}?next={next}&count={count}"

    member _.LikedRootId =
        $"https://{appInfo.ApplicationHostname}/ActivityPub/Liked"

    member _.LikedPageId =
        $"https://{appInfo.ApplicationHostname}/Favorites"

    member _.GetImageUrl(post: Post, blob: PostBlobRef) =
        $"https://{appInfo.ApplicationHostname}/Blobs/UserPosts/{post.Id}/{blob.Id}"

    member _.GetObjectId(post: Post) =
        $"https://{appInfo.ApplicationHostname}/UserPosts/{post.Id}"

    member _.GetObjectId(addressedPost: AddressedPost) =
        $"https://{appInfo.ApplicationHostname}/AddressedPosts/{addressedPost.Id}"

    member _.GetFollowId(followGuid: Guid) =
        $"https://{appInfo.ApplicationHostname}/ActivityPub/Follow/{followGuid}"

    member _.GetLikeId(likeId: Guid) =
        $"https://{appInfo.ApplicationHostname}/ActivityPub/Like/{likeId}"

    member _.GetTransientId() =
        $"https://{appInfo.ApplicationHostname}/#transient-{Guid.NewGuid()}"
