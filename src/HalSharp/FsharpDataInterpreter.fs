module FSharpDataIntepreter

open FSharp.Data

let rec interpret (instance: Hal.AbstractJsonObject<JsonValue>) : JsonValue =
    match instance with
    | Hal.Pure a       -> a
    | Hal.Bool b       -> JsonValue.Boolean b
    | Hal.String s     -> JsonValue.String s
    | Hal.Record map   -> JsonValue.Record (map |> Map.toArray |> Array.map (fun (k,v) -> k, interpret v))
    | Hal.Array a      -> JsonValue.Array (a |> List.map interpret |> List.toArray)