namespace Pandacap.Data

open System
open System.ComponentModel.DataAnnotations.Schema

type GeneralFavorite() =
    inherit GeneralFeedItem()

    [<NotMapped>]
    override this.DisplayAuthor = this.Data.Author

    member val FavoritedAt = DateTimeOffset.MinValue with get, set
    member val HiddenAt = nullDateTimeOffset with get, set

    interface IFavorite with
        member this.HiddenAt
            with get () = this.HiddenAt
             and set value = this.HiddenAt <- value

        member this.FavoritedAt = this.FavoritedAt
