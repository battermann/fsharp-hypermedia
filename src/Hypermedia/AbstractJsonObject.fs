module Hypermedia.Models

/// Represents a minimal generic Json object to describe a hypermedia resource.
type AbstractJsonObject<'a> =
    | JObject of 'a
    | JRecord of Map<string, AbstractJsonObject<'a>>
    | JString of string
    | JArray of AbstractJsonObject<'a> list
    | JBool of bool