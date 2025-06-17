namespace Pandacap.Data

type IBlueskyPost =
    abstract member PDS: string
    abstract member DID: string
    abstract member RecordKey: string
