namespace Pandacap.Data

/// A relationship of some sort between Pandacap and a remote ActivityPub actor. Used in the frontend for paged lists of follows and followers.
type IRemoteActorRelationship =
    abstract member ActorId: string
    abstract member PreferredUsername: string
    abstract member IconUrl: string
    abstract member Pending: bool
