namespace Pandacap.ActivityPub.Inbound

type RemoteActor = {
    Type: string
    Id: string
    Inbox: string
    SharedInbox: string
    PreferredUsername: string
    Url: string
    IconUrl: string
    KeyId: string
    KeyPem: string
}