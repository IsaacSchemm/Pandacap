namespace Pandacap.Data

type IBlueskyPost =
    abstract member CID: string
    abstract member PDS: string
    abstract member DID: string
    abstract member RecordKey: string
    abstract member InFavorites: bool
