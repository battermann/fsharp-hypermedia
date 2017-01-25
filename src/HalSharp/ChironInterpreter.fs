/// Contains the interpreter to transform an `AbstractJsonObject<Chiron.Json>` into a `Chiron.Json`.
module ChironInterpreter

open Chiron

/// Transforms an `AbstractJsonObject<Chiron.Json>` into a `Chiron.Json`.
let rec interpret (instance: Hal.AbstractJsonObject<Json>) : Json =
    match instance with
    | Hal.JObject a    -> a
    | Hal.JBool b       -> Bool b
    | Hal.JString s     -> String s
    | Hal.JRecord map   -> Object (map |> Map.map (fun _ v -> interpret v))
    | Hal.JArray a      -> Array (a |> List.map interpret)