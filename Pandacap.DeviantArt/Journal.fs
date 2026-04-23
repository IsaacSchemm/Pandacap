namespace Pandacap.DeviantArt

open FSharp.Control

open DeviantArtFs.Api.Deviation.Journal

module internal Journal =
    let postAsync token title body tags = task {
        let immutableFields = [Body body]
        let mutableFields = [Title title; for tag in tags do Tag tag]
        return! CreateAsync token immutableFields mutableFields
    }
