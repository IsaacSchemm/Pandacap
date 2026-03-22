namespace Pandacap.ActivityPub.Models

type ActivityPubProfile = {
    Avatars: ActivityPubAvatar list
    Links: ActivityPubProfileLink list
    PublicKeyPem: string
    Username: string
    SummaryHtml: string
}
