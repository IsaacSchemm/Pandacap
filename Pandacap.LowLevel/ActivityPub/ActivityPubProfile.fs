namespace Pandacap.ActivityPub

type ActivityPubProfile = {
    Avatars: ActivityPubAvatar list
    Links: ActivityPubProfileLink list
    PublicKeyPem: string
    Username: string
}
