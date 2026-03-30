[<AutoOpen>]
module internal Shared

open Newtonsoft.Json.Linq
open Pandacap.ActivityPub.JsonLd

/// Given an expanded object and a field name, extracts the JSON array value.
let list str (token: JToken) = token.ExtractArrayElements(str) |> Seq.toList

/// Given a sequence of objects and a field name, extracts the JSON array values for each object and combines them into a single sequence.
let combine name items = Seq.collect (list name) items

/// Given an expanded object, extracts the node identifier as a string.
let node_id (token: JToken) = token.TryExtractStringValue("@id")

/// Given an expanded object, extracts the type(s) as a list of strings.
let node_type (obj: JToken) = [for t in obj |> list "@type" do t.Value<string>()]

/// Given an expanded object, extracts the value as a string.
let node_value (token: JToken) = token.TryExtractStringValue("@value")

/// Given a sequence of objects, returns the result of running the function on the first object, or returns null if the sequence is empty.
let first func seq = seq |> Seq.map func |> Seq.tryHead |> Option.toObj
