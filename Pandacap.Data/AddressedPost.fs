namespace Pandacap.Data

open System
open System.ComponentModel.DataAnnotations.Schema

type AddressedPost() =
    member val Id = Guid.Empty with get, set
    member val InReplyTo = "" with get, set
    member val To = "" with get, set
    member val Cc = new ResizeArray<string>() with get, set
    member val Audience = nullString with get, set
    member val Followers = false with get, set
    member val PublishedTime = DateTimeOffset.MinValue with get, set
    member val HtmlContent = "" with get, set

    [<NotMapped>]
    member this.Recipients = Seq.distinct [
        this.To
        yield! this.Cc
        if not (isNull this.Audience) then this.Audience
    ]
