module ChironInterpreter

open Chiron
open HalSharp

let rec interpret (instance: Instance<Json>) : Json =
    match instance with
    | Pure a       -> a
    | Bool b       -> Chiron.Bool b
    | String s     -> Chiron.String s
    | Instance map -> Chiron.Object (map |> Map.map (fun _ v -> interpret v))
    | Array xs     -> Chiron.Array (xs |> List.map interpret)