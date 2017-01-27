/// Contains the interpreter to transform an `AbstractJsonObject<obj>` into an `obj`.
module ObjectInterpreter

/// Transforms an `AbstractJsonObject<obj>` into an `obj`.
let rec interpret (instance: Hal.AbstractJsonObject<obj>) : obj =
    match instance with
    | Hal.JObject a     -> a
    | Hal.JBool b       -> b :> obj
    | Hal.JString s     -> s :> obj
    | Hal.JRecord map   -> map |> Map.map (fun _ v -> interpret v) :> obj
    | Hal.JArray a      -> a |> List.map interpret :> obj

/// Serializes a HAL resource as `obj`
let toJson resource = Hal.Resource.toJson interpret resource    