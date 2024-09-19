namespace Pandacap.JsonLd

type RemoteAddressee =
| PublicCollection
| Collection of id: string
| Actor of RemoteActor
| InaccessibleObject of id: string
with
    member this.Id =
        match this with
        | PublicCollection -> "https://www.w3.org/ns/activitystreams#Public"
        | Actor a -> a.Id
        | Collection id
        | InaccessibleObject id -> id
