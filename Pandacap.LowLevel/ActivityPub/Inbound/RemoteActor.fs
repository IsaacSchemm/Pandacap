namespace Pandacap.ActivityPub.Inbound

type RemoteActor = {
    Type: string
    Id: string
    Inbox: string
    SharedInbox: string
    PreferredUsername: string
    IconUrl: string
    KeyId: string
    KeyPem: string
}