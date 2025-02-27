namespace Pandacap.Data

open System
open System.ComponentModel.DataAnnotations

/// A remote ActivityPub post that this app's instance owner has reposted.
type ActivityPubAnnounce() =
    inherit ActivityPubFavorite()

    [<Key>]
    member val AnnounceGuid = Guid.Empty with get, set

    override this.Id = this.AnnounceGuid

    interface Pandacap.ActivityPub.ILike with
        member this.ObjectId = this.ObjectId
