namespace Pandacap.CanonicalTags

open FSharp.Control
open Microsoft.EntityFrameworkCore
open Pandacap.Database
open Pandacap.CanonicalTags.Interfaces

type CanonicalTagTreeService(
    pandacapDbContext: PandacapDbContext
) =
    interface ICanonicalTagTreeService with
        member _.GetAllMediumsAsync() = asyncSeq {
            let! token = Async.CancellationToken

            let! items = pandacapDbContext.CanonicalMediums.ToListAsync(token) |> Async.AwaitTask

            for x in items do {
                new ICanonicalTagTreeDisplayNode with
                    member _.Id = x.Id
                    member _.Name = String.concat " " [
                        x.Name
                        if not (isNull x.ShortCode) then $"({x.ShortCode})"
                    ]
                    member _.Children = Seq.empty
            }
        }

        member _.GetAllCharactersAsync() = asyncSeq {
            let! token = Async.CancellationToken

            let! items = pandacapDbContext.CanonicalCharacters.ToListAsync(token) |> Async.AwaitTask

            for x in items do {
                new ICanonicalTagTreeDisplayNode with
                    member _.Id = x.Id
                    member _.Name = String.concat " " [
                        x.Name
                        if not (isNull x.ShortCode) then $"({x.ShortCode})"
                    ]
                    member _.Children = Seq.empty
            }
        }

        member _.GetAllSettingsAsync() = asyncSeq {
            let! token = Async.CancellationToken

            let! items = pandacapDbContext.CanonicalSettings.ToListAsync(token) |> Async.AwaitTask

            for x in items do {
                new ICanonicalTagTreeDisplayNode with
                    member _.Id = x.Id
                    member _.Name = x.Name
                    member _.Children = Seq.empty
            }
        }

        member _.GetAllSpeciesAsync() = asyncSeq {
            let! token = Async.CancellationToken

            let! items = pandacapDbContext.CanonicalSpecies.ToListAsync(token) |> Async.AwaitTask

            let rec toTreeNode (x: CanonicalSpecies) = {
                new ICanonicalTagTreeDisplayNode with
                    member _.Id = x.Id
                    member _.Name = String.concat " " [
                        x.Name
                        if not (isNull x.ShortCode) then $"({x.ShortCode})"
                    ]
                    member _.Children =  [
                        for y in items do
                            let partOfIds = [for p in y.PartOf do p.OtherSpeciesId]
                            if partOfIds |> List.contains x.Id then
                                toTreeNode y
                    ]
            }

            for x in items do if x.PartOf.Count = 0 then toTreeNode x
        }
