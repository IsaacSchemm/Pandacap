namespace Pandacap.Data

type GeneralInboxItem() =
    inherit GeneralFeedItem()

    member val IsShare = false with get, set
    member val DismissedAt = nullDateTimeOffset with get, set

    interface IInboxPost with
        member this.DismissedAt
            with get () = this.DismissedAt
             and set value = this.DismissedAt <- value

        member this.IsPodcast =
            not (isNull this.AudioUrl)

        member _.IsShare = false
