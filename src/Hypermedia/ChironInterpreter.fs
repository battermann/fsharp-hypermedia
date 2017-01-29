/// Contains the interpreter to transform an `JsonModel<Chiron.Json>` into a `Chiron.Json`.
module ChironInterpreter

open Chiron
open Hypermedia

/// Transforms an `JsonModel<Chiron.Json>` into a `Chiron.Json`.
let rec interpret (instance: JsonModel<Json>) : Json =
    match instance with
    | JObject a     -> a
    | JBool b       -> Bool b
    | JString s     -> String s
    | JRecord map   -> Object (map |> Map.map (fun _ v -> interpret v))
    | JArray a      -> Array (a |> List.map interpret)

[<RequireQualifiedAccess>]
module Hal =
    /// Serializes a HAL resource as `Chiron.Json`
    let toJson resource = Hal.Resource.toJson interpret resource    

[<RequireQualifiedAccess>]
module Siren =
    let toJSon entity = Siren.Entity.toJson interpret entity