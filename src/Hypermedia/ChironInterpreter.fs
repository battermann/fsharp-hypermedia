/// Contains the interpreter to transform an `AbstractJsonObject<Chiron.Json>` into a `Chiron.Json`.
module ChironInterpreter

open Chiron

/// Transforms an `AbstractJsonObject<Chiron.Json>` into a `Chiron.Json`.
let rec interpret (instance: Hypermedia.Models.AbstractJsonObject<Json>) : Json =
    match instance with
    | Hypermedia.Models.JObject a     -> a
    | Hypermedia.Models.JBool b       -> Bool b
    | Hypermedia.Models.JString s     -> String s
    | Hypermedia.Models.JRecord map   -> Object (map |> Map.map (fun _ v -> interpret v))
    | Hypermedia.Models.JArray a      -> Array (a |> List.map interpret)

[<RequireQualifiedAccess>]
module Hal =
    /// Serializes a HAL resource as `Chiron.Json`
    let toJson resource = Hal.Resource.toJson interpret resource    

[<RequireQualifiedAccess>]
module Siren =
    let toJSon entity = SirenSerialization.Entity.toJson interpret entity