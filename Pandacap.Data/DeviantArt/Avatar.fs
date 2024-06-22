namespace Pandacap.Data

open System

type Avatar() =
    member val Id = Guid.Empty with get, set
    member val ContentType = "application/octet-stream" with get, set
    member this.BlobName = $"{this.Id}"
