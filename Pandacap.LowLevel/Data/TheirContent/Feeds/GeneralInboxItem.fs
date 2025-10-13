namespace Pandacap.Data

open System.ComponentModel.DataAnnotations.Schema

type GeneralInboxItem() =
    inherit GeneralFeedItem()

    [<NotMapped>]
    override this.DisplayAuthor = this.PostedBy

    member val IsShare = false with get, set
    member val PostedBy = new GeneralFeedItemAuthor() with get, set
    member val DismissedAt = nullDateTimeOffset with get, set

    interface IInboxPost with
        member this.DismissedAt
            with get () = this.DismissedAt
             and set value = this.DismissedAt <- value

        member this.IsPodcast =
            not (isNull this.Data.AudioUrl)

        member this.IsShare = this.IsShare
