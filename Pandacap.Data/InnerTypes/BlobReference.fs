namespace Pandacap.Data

open System
open System.ComponentModel.DataAnnotations.Schema

/// A reference to a blob in Azure Storage that may contain image or thumbnail data.
[<AllowNullLiteral>]
type BlobReference() =

    /// A randomly-generated ID used to name the Azure Storage blob.
    member val Id = Guid.Empty with get, set

    /// The content type of the data (e.g. image/jpeg).
    member val ContentType = "application/octet-stream" with get, set

    /// The name of the blob in Azure Storage that contains the data.
    [<NotMapped>]
    member this.BlobName = $"{this.Id}"
