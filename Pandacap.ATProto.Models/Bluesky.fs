namespace Pandacap.ATProto.Models

open System

type BlueskyProfile = {
    AvatarCID: string
    Labels: string list
    DisplayName: string
    Description: string
} with
    member this.NoUnauthenticated =
        this.Labels |> List.contains "!no-unauthenticated"

type BlueskyImage = {
    CID: string
    Alt: string
}

type BlueskyReplyContext = {
    Parent: ATProtoRef
    Root: ATProtoRef
}

type BlueskyPost = {
    Text: string
    Images: BlueskyImage list
    Quoted: ATProtoRef list
    InReplyTo: BlueskyReplyContext list
    BridgyOriginalUrl: string
    FediverseId: string
    Labels: string list
    CreatedAt: DateTimeOffset
} with
    member this.AdultContent = Seq.head (seq {
        for label in this.Labels do
            match label with
            | "porn"
            | "sexual"
            | "nudity"
            | "sexual-figurative"
            | "graphic-media" -> true
            | _ -> ()

        false
    })

type BlueskyInteraction = {
    CreatedAt: DateTimeOffset
    Subject: ATProtoRef
}