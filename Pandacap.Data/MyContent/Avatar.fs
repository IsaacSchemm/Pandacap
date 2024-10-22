namespace Pandacap.Data

open System
open System.ComponentModel.DataAnnotations.Schema

/// The user's current avatar.
[<AllowNullLiteral>]
type Avatar() =
    member val Id = Guid.Empty with get, set
    member val ContentType = "application/octet-stream" with get, set

    /// The name of the blob in Azure Storage that contains the avatar data.
    [<NotMapped>]
    member this.BlobName = $"{this.Id}"
