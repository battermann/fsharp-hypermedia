
/// Contains the interpreter to transform an `AbstractJsonObject<FSharp.Data.JsonValue>` into a `FSharp.Data.JsonValue`.
module FSharpDataIntepreter

open FSharp.Data

/// Transforms an `AbstractJsonObject<FSharp.Data.JsonValue>` into a `FSharp.Data.JsonValue`.
let rec interpret (instance: Hal.AbstractJsonObject<JsonValue>) : JsonValue =
    match instance with
    | Hal.JObject a     -> a
    | Hal.JBool b       -> JsonValue.Boolean b
    | Hal.JString s     -> JsonValue.String s
    | Hal.JRecord map   -> JsonValue.Record (map |> Map.toArray |> Array.map (fun (k,v) -> k, interpret v))
    | Hal.JArray a      -> JsonValue.Array (a |> List.map interpret |> List.toArray)