namespace Pandacap.LowLevel

open System

type DeviantArtImportParameters = {
    id: Guid
    altText: string
}

type DeviantArtImportScope =
| All
| Recent of cutoff: DateTimeOffset
| Single of parameters: DeviantArtImportParameters
