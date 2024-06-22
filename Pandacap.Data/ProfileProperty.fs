namespace Pandacap.Data

open System

/// A name/value pair associated with the instance owner's ActivityPub profile.
type ProfileProperty() =
    member val Id = Guid.Empty with get, set
    member val Name = "" with get, set
    member val Value = "" with get, set
    member val Link = nullString with get, set
