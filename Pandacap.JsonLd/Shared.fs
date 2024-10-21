[<AutoOpen>]
module Shared

open Newtonsoft.Json.Linq

/// Given an expanded object and a field name, extracts the JSON array value.
let list (name: string) (obj: JToken) =
    obj[name]
    |> Option.ofObj
    |> Option.map Seq.toList
    |> Option.defaultValue []

/// Given a sequence of objects and a field name, extracts the JSON array values for each object and combines them into a single sequence.
let combine name items = Seq.collect (list name) items

/// Given an expanded object, extracts the node identifier as a string.
let node_id (obj: JToken) = (obj["@id"]).Value<string>()

/// Given an expanded object, extracts the type as a string.
let node_type (obj: JToken) =
    if isNull (obj["@type"])
    then Seq.empty
    else (obj["@type"]).Values<string>()

/// Given an expanded object, extracts the value as a string.
let node_value (obj: JToken) = (obj["@value"]).Value<string>()

/// Given a sequence of objects, returns the result of running the function on the first object, or returns null if the sequence is empty.
let first func arr = arr |> Seq.map func |> Seq.tryHead |> Option.toObj
