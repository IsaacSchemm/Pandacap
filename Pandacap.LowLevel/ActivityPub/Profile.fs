namespace Pandacap.ActivityPub

open Pandacap.LowLevel.MyLinks

type Profile = {
    Avatars: Avatar list
    Links: MyLink list
    PublicKeyPem: string
    Username: string
}
