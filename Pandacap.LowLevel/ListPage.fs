namespace Pandacap.LowLevel

open Pandacap.Data

type ListPage = {
    Current: IPost list
    Next: string list
} with
    interface Pandacap.ActivityPub.IListPage with
        member this.Current = [for x in this.Current do x :> obj]
        member this.Next =
            match this.Next with
            | [] -> Pandacap.ActivityPub.NoNextItem
            | id :: _ -> Pandacap.ActivityPub.NextItem id
