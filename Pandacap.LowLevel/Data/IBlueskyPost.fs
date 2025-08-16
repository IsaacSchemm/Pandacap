namespace Pandacap.Data

type IBlueskyPost =
    abstract member PDS: string
    abstract member DID: string
    abstract member RecordKey: string
    abstract member Liked: bool
    abstract member InFavorites: bool
