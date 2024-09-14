namespace Pandacap.Data

open System

type Reply() =
    member val Id = Guid.Empty with get, set
    member val InReplyTo = "" with get, set
    member val To = new ResizeArray<string>() with get, set
    member val Cc = new ResizeArray<string>() with get, set
    member val PublishedTime = DateTimeOffset.MinValue with get, set
    member val Content = "" with get, set
