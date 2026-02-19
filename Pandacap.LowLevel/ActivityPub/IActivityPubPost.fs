namespace Pandacap.ActivityPub

open System

type IActivityPubPost =
    abstract member GetObjectId: hostInfo: ActivityPubHostInformation -> string
    abstract member GetAddressing: hostInfo: ActivityPubHostInformation -> IActivityPubAddressing
    abstract member PublishedTime: DateTimeOffset
    abstract member IsJournal: bool
    abstract member Title: string
    abstract member Html: string
    abstract member Tags: string seq
    abstract member Links: IActivityPubLink seq
    abstract member Images: IActivityPubImage seq
