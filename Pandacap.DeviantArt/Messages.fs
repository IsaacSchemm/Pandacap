namespace Pandacap.DeviantArt

open DeviantArtFs.Api.Messages

module internal Messages =
    let getInboxFeedAsync token =
        GetFeedAsync token (StackMessages false) Inbox StartingCursor
