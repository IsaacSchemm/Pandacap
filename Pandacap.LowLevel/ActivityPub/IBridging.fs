namespace Pandacap.ActivityPub

type IBridging =
    abstract member BlueskyDID: string with get, set
    abstract member BlueskyRecordKey: string with get, set
