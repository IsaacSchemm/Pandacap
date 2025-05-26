namespace Pandacap.Data

/// An artwork submission posted by a user who this instance's owner follows on DeviantArt.
type InboxArtworkDeviation() =
    inherit InboxDeviation()

    member val ThumbnailUrl = "" with get, set

    override this.ThumbnailUrls = List.choose Option.ofObj [this.ThumbnailUrl]
