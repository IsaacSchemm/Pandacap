namespace Pandacap.Data

open System
open System.ComponentModel.DataAnnotations

/// A remote ActivityPub post that this app's instance owner has liked.
type ActivityPubLike() =
    inherit ActivityPubFavorite()

    [<Key>]
    member val LikeGuid = Guid.Empty with get, set

    override this.Id = this.LikeGuid

    interface Pandacap.ActivityPub.ILike with
        member this.ObjectId = this.ObjectId
