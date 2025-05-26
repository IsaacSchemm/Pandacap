namespace Pandacap.ActivityPub

open System

type IPost =
    abstract member Id: Guid
    abstract member IsJournal: bool
    abstract member Title: string
    abstract member Html: string
    abstract member Tags: string seq
    abstract member PublishedTime: DateTimeOffset
    abstract member Images: IImage seq
