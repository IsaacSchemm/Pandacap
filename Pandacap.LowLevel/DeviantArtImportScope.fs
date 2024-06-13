namespace Pandacap.LowLevel

open System

type DeviantArtImportScope =
| All
| Recent of cutoff: DateTimeOffset
| Subset of ids: Set<Guid>
with
    static member FromIds seq = Subset (set seq)