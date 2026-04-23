namespace Pandacap.DeviantArt

open DeviantArtFs.ParameterTypes

open DeviantArtFs.Api.Notes

module internal Notes =
    let getInboxAsync token =
        GetNotesAsync token Inbox DefaultPagingLimit StartingOffset
