namespace Pandacap.ActivityPub

type IAddressing =
    abstract member InReplyTo: string
    abstract member To: string seq
    abstract member Cc: string seq
    abstract member Audience: string
