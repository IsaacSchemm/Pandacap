namespace Pandacap.ActivityPub.Models.Inbound

open Pandacap.ActivityPub.Signatures.Interfaces

type RemoteActor = {
    Type: string
    Id: string
    Inbox: string
    SharedInbox: string
    PreferredUsername: string
    Name: string
    Summary: string
    Url: string
    IconUrl: string
    KeyId: string
    KeyPem: string
} with
    interface IKey with
        member this.KeyId = this.KeyId
        member this.KeyPem = this.KeyPem
