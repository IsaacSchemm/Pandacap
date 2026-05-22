namespace Pandacap.DeviantArt

open DeviantArtFs.ParameterTypes

open DeviantArtFs.Api.Stash

module internal Artwork =
    let asyncPostArtwork token data contentType title artistComments tags galleryFolders isAIGenerated noThirdPartyAI = async {
        let submissionParameters = {
            title = SubmissionTitle title
            artist_comments = ArtistComments artistComments
            tags = TagList [yield! tags]
            original_url = NoOriginalUrl
            is_dirty = false
        }

        let formFile = {
            new IFormFile with
                member _.ContentType = contentType
                member _.Data = data
                member _.Filename =
                    let extension =
                        match contentType.Split('/') with
                        | [| _; subtype|] -> subtype
                        | _ -> "dat"
                    $"file.{extension}"
        }

        let! stashResult = AsyncSubmit token (SubmitToStack RootStack) submissionParameters formFile

        let publishParameters = [
            for folder in galleryFolders do
                GalleryId folder
            Maturity NotMature
            AllowComments true
            AllowFreeDownload true
            for tag in tags do
                Tag tag
            if isAIGenerated then IsAiGenerated else IsNotAiGenerated
            if noThirdPartyAI then NoThirdPartyAi else ThirdPartyAiOk
        ]

        return! AsyncPublish token publishParameters (Item stashResult.itemid)
    }
