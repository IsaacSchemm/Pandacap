namespace Pandacap.Data

open Pandacap.PlatformBadges

type IFollow =
    abstract member Platform: PostPlatform with get
    abstract member Url: string with get
    abstract member LinkUrl: string with get
    abstract member Username: string with get
    abstract member IconUrl: string with get
    abstract member Filtered: bool with get
