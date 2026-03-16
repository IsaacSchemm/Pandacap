namespace Pandacap.ActivityPub.Inbound

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
}