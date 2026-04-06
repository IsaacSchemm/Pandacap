namespace Pandacap.ATProto.Models

type DIDDocument = {
    DID: string
    Handles: string list
    PDSes: string list
} with
    member this.Handle = List.head this.Handles
    member this.PDS = List.head this.PDSes
