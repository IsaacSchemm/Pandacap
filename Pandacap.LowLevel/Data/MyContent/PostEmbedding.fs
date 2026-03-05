namespace Pandacap.Data

open System

type PostEmbedding() =
    member val Id = Guid.Empty with get, set

    member val Model = nullString with get, set

    member val Text: ResizeArray<float32> = null with get, set
    //member val AdditionalText: ResizeArray<float32> = null with get, set
    //member val Tags: ResizeArray<float32> = null with get, set

    //member val Image: ResizeArray<float32> = null with get, set
    //member val ImageAltText: ResizeArray<float32> = null with get, set

    member val PublishedTime = DateTimeOffset.MinValue with get, set
