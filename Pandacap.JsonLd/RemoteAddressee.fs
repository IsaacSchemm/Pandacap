namespace Pandacap.JsonLd

type RemoteAddressee =
| PublicCollection
| Collection of id: string
| Actor of RemoteActor
| InaccessibleObject of id: string
with
    member this.Type =
        match this with
        | PublicCollection -> "Public"
        | Actor a -> a.Type.Replace("https://www.w3.org/ns/activitystreams#", "")
        | Collection _ -> "Collection"
        | InaccessibleObject _ -> "Inaccessible or malformed actor object"
    member this.Id =
        match this with
        | PublicCollection -> "https://www.w3.org/ns/activitystreams#Public"
        | Actor a -> a.Id
        | Collection id
        | InaccessibleObject id -> id
