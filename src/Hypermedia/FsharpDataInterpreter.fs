
/// Contains the interpreter to transform an `AbstractJsonObject<FSharp.Data.JsonValue>` into a `FSharp.Data.JsonValue`.
module FSharpDataIntepreter

open FSharp.Data

/// Transforms an `AbstractJsonObject<FSharp.Data.JsonValue>` into a `FSharp.Data.JsonValue`.
let rec internal interpret (instance: Hypermedia.Models.AbstractJsonObject<JsonValue>) : JsonValue =
    match instance with
    | Hypermedia.Models.JObject a     -> a
    | Hypermedia.Models.JBool b       -> JsonValue.Boolean b
    | Hypermedia.Models.JString s     -> JsonValue.String s
    | Hypermedia.Models.JRecord map   -> JsonValue.Record (map |> Map.toArray |> Array.map (fun (k,v) -> k, interpret v))
    | Hypermedia.Models.JArray a      -> JsonValue.Array (a |> List.map interpret |> List.toArray)

/// Serializes a HAL resource as `FSharp.Data.JsonValue`
let toJson resource = Hal.Resource.toJson interpret resource