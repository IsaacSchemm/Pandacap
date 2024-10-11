namespace Pandacap.Types

open System

type DeviantArtImportScope =
| Window of oldest: DateTimeOffset * newest: DateTimeOffset
| Subset of ids: Set<Guid>
with
    static member FromIds seq = Subset (set seq)