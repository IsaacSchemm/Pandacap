namespace Pandacap.LowLevel.Txt

open System

type Twt = {
    timestamp: DateTimeOffset
    text: string
    replyContext: ReplyContext
}
