namespace Pandacap.Data

open Pandacap.PlatformBadges

type IExternalCredentials =
    abstract member Username: string
    abstract member Platform: PostPlatform
