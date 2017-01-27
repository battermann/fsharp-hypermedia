/// Contains the interpreter to transform an `AbstractJsonObject<obj>` into an `obj`.
module ObjectInterpreter

/// Transforms an `AbstractJsonObject<obj>` into an `obj`.
let rec interpret (instance: Hypermedia.Models.AbstractJsonObject<obj>) : obj =
    match instance with
    | Hypermedia.Models.JObject a     -> a
    | Hypermedia.Models.JBool b       -> b :> obj
    | Hypermedia.Models.JString s     -> s :> obj
    | Hypermedia.Models.JRecord map   -> map |> Map.map (fun _ v -> interpret v) :> obj
    | Hypermedia.Models.JArray a      -> a |> List.map interpret :> obj

/// Serializes a HAL resource as `obj`
let toJson resource = Hal.Resource.toJson interpret resource    