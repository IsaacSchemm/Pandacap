namespace Pandacap.Data

open System
open Pandacap.PlatformBadges

/// A twtxt feed that is followed by the instance owner.
type TwtxtFeed() =
    member val Id = Guid.Empty with get, set
    member val Url = "" with get, set
    member val Nick = nullString with get, set
    member val Avatar = nullString with get, set
    member val Refresh = TimeSpan.Zero with get, set
    member val LastCheckedAt = DateTimeOffset.MinValue with get, set

    interface IFollow with
        member _.Platform = Twtxt
        member this.IconUrl = this.Avatar
        member this.Username = this.Nick |> orString this.Url
        member this.Url = this.Url
