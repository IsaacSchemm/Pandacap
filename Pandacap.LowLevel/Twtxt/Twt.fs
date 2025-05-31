namespace Pandacap.LowLevel.Twtxt

open System

type Twt = {
    timestamp: DateTimeOffset
    text: string
    replyContext: ReplyContext
}
