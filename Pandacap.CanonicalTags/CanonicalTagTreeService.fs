namespace Pandacap.CanonicalTags

open System
open Microsoft.EntityFrameworkCore
open FSharp.Control
open Pandacap.Database
open Pandacap.CanonicalTags.Interfaces

type CanonicalTagTreeService(
    pandacapDbContext: PandacapDbContext
) =
    interface ICanonicalTagTreeService with
        member _.GetAllTagsAsync() = asyncSeq {
            let! token = Async.CancellationToken

            let! mediums = pandacapDbContext.CanonicalMediums.ToListAsync(token) |> Async.AwaitTask

            {
                new ICanonicalTagTreeDisplayNode with
                    member _.Id = Nullable()
                    member _.Name = "Mediums"
                    member _.Type = CanonicalTagType.Category
                    member _.Children = [
                        for medium in mediums |> Seq.sortBy (fun x -> x.Name) do {
                            new ICanonicalTagTreeDisplayNode with
                                member _.Id = Nullable(medium.Id)
                                member _.Name = medium.Name
                                member _.Type = CanonicalTagType.Medium
                                member _.Children = Seq.empty
                        }
                    ]
            }

            let! characters = pandacapDbContext.CanonicalCharacters.ToListAsync(token) |> Async.AwaitTask
            let! settings = pandacapDbContext.CanonicalSettings.ToListAsync(token) |> Async.AwaitTask

            let characterToTreeNode (this: CanonicalCharacter) = {
                new ICanonicalTagTreeDisplayNode with
                    member _.Id = Nullable(this.Id)
                    member _.Name = this.Name
                    member _.Type = CanonicalTagType.Character
                    member _.Children = []
            }

            {
                new ICanonicalTagTreeDisplayNode with
                    member _.Id = Nullable()
                    member _.Name = "Settings & Characters"
                    member _.Type = CanonicalTagType.Category
                    member _.Children = [
                        for setting in settings |> Seq.sortBy (fun x -> x.Name) do {
                            new ICanonicalTagTreeDisplayNode with
                                member _.Id = Nullable(setting.Id)
                                member _.Name = setting.Name
                                member _.Type = CanonicalTagType.Setting
                                member _.Children = [
                                    for character in characters |> Seq.sortBy (fun x -> x.Name) do
                                        if character.SettingId = Nullable(setting.Id) then
                                            characterToTreeNode character
                                ]
                        }

                        for character in characters |> Seq.sortBy (fun x -> x.Name) do
                            if character.SettingId = Nullable() then
                                characterToTreeNode character
                    ]
            }

            let! allSpecies = pandacapDbContext.CanonicalSpecies.ToListAsync(token) |> Async.AwaitTask

            let rec speciesToTreeNode (this: CanonicalSpecies) = {
                new ICanonicalTagTreeDisplayNode with
                    member _.Id = Nullable(this.Id)
                    member _.Name = this.Name
                    member _.Type = CanonicalTagType.Species
                    member _.Children =  [
                        for potentialChild in allSpecies |> Seq.sortBy (fun x -> x.Name) do
                            if potentialChild.PartOf |> Seq.map (fun p -> p.OtherSpeciesId) |> Seq.contains this.Id then
                                speciesToTreeNode potentialChild
                    ]
            }

            {
                new ICanonicalTagTreeDisplayNode with
                    member _.Id = Nullable()
                    member _.Name = "Species"
                    member _.Type = CanonicalTagType.Category
                    member _.Children = [
                        for x in allSpecies |> Seq.sortBy (fun x -> x.Name) do
                            if x.PartOf.Count = 0 then
                                speciesToTreeNode x
                    ]
            }
        }
