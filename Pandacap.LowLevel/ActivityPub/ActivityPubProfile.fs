namespace Pandacap.ActivityPub

open Pandacap.LowLevel.MyLinks

type ActivityPubProfile = {
    Avatars: ActivityPubAvatar list
    Links: MyLink list
    PublicKeyPem: string
    Username: string
}
