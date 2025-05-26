namespace Pandacap.ActivityPub

open System

type IImage =
    abstract member AltText: string
    abstract member BlobId: Guid
    abstract member MediaType: string
    abstract member HorizontalFocalPoint: decimal option
    abstract member VerticalFocalPoint: decimal option
