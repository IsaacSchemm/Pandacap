namespace Pandacap.ActivityPub.Inbound

type RemoteAddressee =
| PublicCollection
| Collection of id: string
| Actor of RemoteActor
| UnauthorizedObject of id: string
| InaccessibleObject of id: string
with
    member this.Type =
        match this with
        | PublicCollection -> "Public"
        | Actor a -> a.Type.Replace("https://www.w3.org/ns/activitystreams#", "")
        | Collection _ -> "Collection"
        | UnauthorizedObject _ -> "Unauthorized"
        | InaccessibleObject _ -> "Inaccessible"
    member this.Id =
        match this with
        | PublicCollection -> "https://www.w3.org/ns/activitystreams#Public"
        | Actor a -> a.Id
        | Collection id
        | UnauthorizedObject id -> id
        | InaccessibleObject id -> id
