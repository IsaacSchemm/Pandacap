﻿namespace Pandacap.Data

open System
open System.ComponentModel.DataAnnotations.Schema

/// The user's current avatar.
type Avatar() =

    /// A randomly-generated ID used to name the Azure Storage blob.
    member val Id = Guid.Empty with get, set

    /// The content type of the avatar data (e.g. image/jpeg).
    member val ContentType = "application/octet-stream" with get, set

    /// The name of the blob in Azure Storage that contains the avatar data.
    [<NotMapped>]
    member this.BlobName = $"{this.Id}"
