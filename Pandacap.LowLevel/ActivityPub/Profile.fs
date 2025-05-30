namespace Pandacap.ActivityPub

open Pandacap.LowLevel.MyLinks

type Profile = {
    Avatar: Avatar
    Links: MyLink list
    PublicKeyPem: string
    Username: string
}
