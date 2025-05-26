namespace Pandacap.ActivityPub

open System

type IAddressedPost =
    abstract member Id: Guid
    abstract member Title: string
    abstract member Html: string
    abstract member InReplyTo: string
    abstract member PublishedTime: DateTimeOffset
    abstract member To: string seq
    abstract member Cc: string seq
    abstract member Audience: string
