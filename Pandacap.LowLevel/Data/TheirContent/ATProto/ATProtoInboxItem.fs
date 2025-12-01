namespace Pandacap.Data

type ATProtoInboxItem() =
    inherit ATProtoFeedItem()

    member val DismissedAt = nullDateTimeOffset with get, set

    interface IInboxPost with
        member this.DismissedAt
            with get () = this.DismissedAt
             and set value = this.DismissedAt <- value

        member _.IsPodcast = false
        member _.IsShare = false
        member this.OriginalAuthors = [this]
