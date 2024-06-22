namespace Pandacap.Data

type IRemoteActorRelationship =
    abstract member ActorId: string
    abstract member PreferredUsername: string
    abstract member IconUrl: string
    abstract member Pending: bool
