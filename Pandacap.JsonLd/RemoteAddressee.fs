namespace Pandacap.JsonLd

type RemoteAddressee =
| PublicCollection
| Collection of id: string
| Actor of RemoteActor
with
    member this.Id =
        match this with
        | PublicCollection -> "https://www.w3.org/ns/activitystreams#Public"
        | Actor a -> a.Id
        | Collection id -> id
