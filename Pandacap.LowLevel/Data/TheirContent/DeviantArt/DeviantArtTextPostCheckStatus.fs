namespace Pandacap.Data

open System
open System.ComponentModel.DataAnnotations

/// An object to keep track of the last time posts were imported to the inbox.
type DeviantArtTextPostCheckStatus() =
    [<Key>]
    member val Id = Guid.Empty with get, set

    /// The date/time at which Pandacap last checked for DeviantArt text posts
    /// from followed users. Any users who haven't visited DeviantArt since
    /// this time will be skipped.
    member val LastCheck = DateTimeOffset.MinValue with get, set
