namespace Pandacap.DeviantArt

open DeviantArtFs.Api.User

module internal Status =
    let asyncPost token body = async {
        let content = {
            object = Nothing
            parent = NoParent
            stash_item = NoStashItem
        }

        return! AsyncPostStatus token content body
    }
