namespace Pandacap.Data

open System

[<AllowNullLiteral>]
type BlobReference() =
    member val Id = Guid.Empty with get, set
    member val ContentType = "application/octet-stream" with get, set
    member this.BlobName = $"{this.Id}"
