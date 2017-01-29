
/// Contains the interpreter to transform an `JsonModel<FSharp.Data.JsonValue>` into a `FSharp.Data.JsonValue`.
module FSharpDataIntepreter

open FSharp.Data
open Hypermedia

/// Transforms an `JsonModel<FSharp.Data.JsonValue>` into a `FSharp.Data.JsonValue`.
let rec internal interpret (instance: JsonModel<JsonValue>) : JsonValue =
    match instance with
    | JObject a     -> a
    | JBool b       -> JsonValue.Boolean b
    | JString s     -> JsonValue.String s
    | JRecord map   -> JsonValue.Record (map |> Map.toArray |> Array.map (fun (k,v) -> k, interpret v))
    | JArray a      -> JsonValue.Array (a |> List.map interpret |> List.toArray)

[<RequireQualifiedAccess>]
module Hal =
    /// Serializes a HAL resource as `FSharp.Data.JsonValue`
    let toJson resource = Hal.Resource.toJson interpret resource

[<RequireQualifiedAccess>]
module Siren =
    let toJSon entity = Siren.Entity.toJson interpret entity