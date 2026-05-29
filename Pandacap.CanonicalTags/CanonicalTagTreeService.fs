namespace Pandacap.CanonicalTags

open System
open Microsoft.EntityFrameworkCore
open FSharp.Control
open Pandacap.Database
open Pandacap.CanonicalTags.Models
open Pandacap.CanonicalTags.Interfaces

type internal CanonicalTagTreeService(
    pandacapDbContext: PandacapDbContext
) =
    interface ICanonicalTagTreeService with
        member _.GetAllTagsAsync() = asyncSeq {
            let! token = Async.CancellationToken

            let! mediums = pandacapDbContext.CanonicalMediums.ToListAsync(token) |> Async.AwaitTask

            yield {
                Id = Nullable()
                Name = "Mediums"
                Type = CanonicalTagType.Category
                Children = [
                    for medium in mediums |> Seq.sortBy (fun x -> x.Name) do {
                        Id = Nullable(medium.Id)
                        Name = medium.Name
                        Type = CanonicalTagType.Medium
                        Children = []
                    }
                ]
            }

            let! characters = pandacapDbContext.CanonicalCharacters.ToListAsync(token) |> Async.AwaitTask
            let! settings = pandacapDbContext.CanonicalSettings.ToListAsync(token) |> Async.AwaitTask

            let characterToTreeNode (this: CanonicalCharacter) = {
                Id = Nullable(this.Id)
                Name = String.concat " " [
                    this.Name
                    if this.Original then "(OC)"
                    if this.Fan then "(Fanart)"
                    if not (isNull this.ShortCode) then "*"
                ]
                Type = CanonicalTagType.Character
                Children = []
            }

            yield {
                Id = Nullable()
                Name = "Settings & Characters"
                Type = CanonicalTagType.Category
                Children = [
                    for this in settings |> Seq.sortBy (fun x -> x.Name) do {
                        Id = Nullable(this.Id)
                        Name = String.concat " " [
                            this.Name
                            if this.Original then "(OC)"
                            if this.Fan then "(Fanart)"
                        ]
                        Type = CanonicalTagType.Setting
                        Children = [
                            for character in characters |> Seq.sortBy (fun x -> x.Name) do
                                if character.SettingId = Nullable(this.Id) then
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
                Id = Nullable(this.Id)
                Name = String.concat " " [
                    this.Name
                    if this.Original then "(OC)"
                    if this.Fan then "(Fanart)"
                    if not (isNull this.ShortCode) then "*"
                ]
                Type = CanonicalTagType.Species
                Children =  [
                    for potentialChild in allSpecies |> Seq.sortBy (fun x -> x.Name) do
                        if potentialChild.PartOf |> Seq.map (fun p -> p.OtherSpeciesId) |> Seq.contains this.Id then
                            speciesToTreeNode potentialChild
                ]
            }

            yield {
                Id = Nullable()
                Name = "Species"
                Type = CanonicalTagType.Category
                Children = [
                    for x in allSpecies |> Seq.sortBy (fun x -> x.Name) do
                        if x.PartOf.Count = 0 then
                            speciesToTreeNode x
                ]
            }
        }
