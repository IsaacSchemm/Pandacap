namespace Pandacap.Data

/// A journal or status update posted by a user who this instance's owner follows on DeviantArt.
type InboxTextDeviation() =
    inherit InboxDeviation()

    override _.ThumbnailUrls = Seq.empty
