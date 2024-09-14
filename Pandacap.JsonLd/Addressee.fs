namespace Pandacap.JsonLd

type Addressee =
| Public
| Person of RemoteActor
| Group of RemoteActor
| Other of id: string
with
    member this.IconUrl =
        match this with
        | Person a | Group a -> a.IconUrl
        | _ -> null
    member this.Name =
        match this with
        | Public -> "Public"
        | Person a | Group a -> a.PreferredUsername
        | Other id -> id
    member this.Url =
        match this with
        | Public -> null
        | Person a | Group a -> a.Id
        | Other id -> id