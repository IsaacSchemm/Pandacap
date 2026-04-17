namespace Pandacap.ATProto.Models

open System

type BlueskyProfile = {
    AvatarCID: string
    DisplayName: string
    Description: string
}

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
}

type BlueskyInteraction = {
    CreatedAt: DateTimeOffset
    Subject: ATProtoRef
}