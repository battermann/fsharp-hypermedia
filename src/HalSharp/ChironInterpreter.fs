module ChironInterpreter

open Chiron

let rec interpret (instance: Hal.AbstractJsonObject<Json>) : Json =
    match instance with
    | Hal.Pure a       -> a
    | Hal.Bool b       -> Bool b
    | Hal.String s     -> String s
    | Hal.Instance map -> Object (map |> Map.map (fun _ v -> interpret v))
    | Hal.Array a      -> Array (a |> List.map interpret)