namespace Pandacap.ActivityPub

open System

type IPost =
    abstract member GetObjectId: hostInfo: HostInformation -> string
    abstract member GetAddressing: hostInfo: HostInformation -> IAddressing
    abstract member PublishedTime: DateTimeOffset
    abstract member IsJournal: bool
    abstract member Title: string
    abstract member Html: string
    abstract member Tags: string seq
    abstract member Images: IImage seq
    abstract member Bridging: IBridging
