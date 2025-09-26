namespace Pandacap.ActivityPub

type HostInformation = {
    ApplicationHostname: string
    ApplicationName: string
    WebsiteUrl: string
} with
    member this.ActivityPubFollowersRootId = $"https://{this.ApplicationHostname}/ActivityPub/Following"
