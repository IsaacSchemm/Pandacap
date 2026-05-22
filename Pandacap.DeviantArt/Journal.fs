namespace Pandacap.DeviantArt

open DeviantArtFs.Api.Deviation.Journal

module internal Journal =
    let asyncPost token title body tags = async {
        let immutableFields = [Body body]
        let mutableFields = [Title title; for tag in tags do Tag tag]
        return! AsyncCreate token immutableFields mutableFields
    }
