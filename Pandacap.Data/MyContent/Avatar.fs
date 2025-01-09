namespace Pandacap.Data

open System
open System.ComponentModel.DataAnnotations.Schema

/// The user's current avatar.
type Avatar() =
    member val Id = Guid.Empty with get, set
    member val ContentType = "application/octet-stream" with get, set

    /// The name of the blob in Azure Storage that contains the avatar data.
    [<NotMapped>]
    member this.BlobName = $"{this.Id}"

    interface Pandacap.ActivityPub.IAvatar with
        member this.Id = this.Id
        member this.MediaType = this.BlobName
