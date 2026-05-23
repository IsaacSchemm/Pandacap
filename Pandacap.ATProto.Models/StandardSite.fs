namespace Pandacap.ATProto.Models

open System

type Icon = {
    CID: string
}

type StandardSitePublication = {
    Url: string
    Icon: Icon option
    Name: string
    Description: string option
} with
    member this.Icons = Option.toList this.Icon
    member this.TextBodies = Option.toList this.Description

type StandardSiteSite =
| Publication of ATProtoRefUri
| Loose of string
with
    member this.OriginalString =
        match this with
        | Publication { Raw = str } -> str
        | Loose str -> str

type StandardSiteDocument = {
    Site: StandardSiteSite
    Path: string option
    Title: string
    Description: string option
    TextContent: string option
    Tags: string list
    PublishedAt: DateTimeOffset
    UpdatedAt: DateTimeOffset option
} with
    member this.CanonicalURLs =
        this.Path
        |> Option.map (fun p -> $"{this.Site.OriginalString}/{p}")
        |> Option.toList

    member this.TextBodies = List.choose id [
        this.Description
        this.TextContent
    ]
