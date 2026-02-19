namespace Pandacap.ActivityPub

type IActivityPubAddressing =
    abstract member InReplyTo: string
    abstract member To: string seq
    abstract member Cc: string seq
    abstract member Audience: string
