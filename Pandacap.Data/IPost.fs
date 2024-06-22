namespace Pandacap.Data

open System

type IPost =
    abstract member Id: string
    abstract member Username: string
    abstract member Usericon: string
    abstract member DisplayTitle: string
    abstract member Timestamp: DateTimeOffset
    abstract member LinkUrl: string
    abstract member Images: IPostImage seq 
