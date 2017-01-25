/// Define Hypertext Application Language (HAL) resources.
/// Transform HAL resource to specific Json implementations.
module Hal

open System

/// Represents a minimal generic Json object to describe a HAL resource.
type AbstractJsonObject<'a> =
    | JObject of 'a
    | JRecord of Map<string, AbstractJsonObject<'a>>
    | JString of string
    | JArray of AbstractJsonObject<'a> list
    | JBool of bool

/// A link representation according to the HAL specification (https://tools.ietf.org/html/draft-kelly-json-hal-08).
type Link = {
    href: string
    templated: bool option
    mediaType: string option
    deprication: Uri option
    name: string option
    profile: Uri option
    title: string option
    hreflang: string option
}

/// A resource representation according to the HAL specification (https://tools.ietf.org/html/draft-kelly-json-hal-08).
type Resource<'a> = {
    links: Map<string, Link list>
    embedded: Map<string, Resource<'a> list>
    properties: Map<string, AbstractJsonObject<'a>>
}

[<RequireQualifiedAccess>]
[<CompilationRepresentation (CompilationRepresentationFlags.ModuleSuffix)>]
module internal Link =
    let simple href = {
        href = href
        templated = None
        mediaType = None
        deprication = None
        name = None
        profile = None
        title = None
        hreflang = None 
    }

    let serializeSingleLink link : AbstractJsonObject<'a> =
        Map.ofList
            [ yield ("href", JString link.href)
              yield! match link.templated with Some b -> [ "templated", JBool b ] | _ -> []
              yield! match link.mediaType with Some mt -> [ "type", JString mt ] | _ -> []
              yield! match link.deprication with Some dep -> [ "deprication", JString (dep.ToString()) ] | _ -> []
              yield! match link.name with Some name -> [ "name", JString name ] | _ -> []
              yield! match link.profile with Some prof -> [ "profile", JString (prof.ToString()) ] | _ -> []
              yield! match link.title with Some title -> [ "title", JString title ] | _ -> []
              yield! match link.hreflang with Some lang -> [ "hreflang", JString lang ] | _ -> [] ]
            |> JRecord

    let serialize (links: Map<string, Link list>) : AbstractJsonObject<'a> option =
        let serializeLinkList links =
            match links |> List.length with
            | 1 -> serializeSingleLink (links |> List.head)
            | _ -> JArray (links |> List.map serializeSingleLink)

        let nonEmptyLinks = links |> Map.filter (fun _ linkList -> not (List.isEmpty linkList))

        if nonEmptyLinks |> Map.isEmpty then
            None
        else
            nonEmptyLinks
            |> Map.map (fun _ linkList -> serializeLinkList linkList)
            |> JRecord
            |> Some

/// Contains functions to transform resources to Json representations.
[<RequireQualifiedAccess>]
module Resource =

    /// Returns an empty resource object the represents a valid HAL resource.
    let empty = {
        links = Map.empty
        embedded = Map.empty
        properties = Map.empty
    }

    let rec internal serialize resource =
        let merge (maps: Map<_,_> seq): Map<_,_> = 
            List.concat (maps |> Seq.map Map.toList) |> Map.ofList
        
        let embedded =
            let serializeEmbedded resources =
                match resources |> List.length with
                | 0 -> JRecord Map.empty
                | 1 -> serialize (resources |> List.head)
                | _ -> JArray (resources |> List.map serialize)

            let embeddedMap = 
                resource.embedded
                |> Map.map (fun rel res -> serializeEmbedded res)
                
            if embeddedMap |> Map.isEmpty then
                Map.empty
            else
                Map.ofList [ "_embedded", embeddedMap |> JRecord ]

        let links = 
            match Link.serialize resource.links with
            | Some ls -> Map.ofList [ "_links", ls ]
            | _       -> Map.empty

        [ links; resource.properties; embedded ]
        |> merge
        |> JRecord

    /// Serializes a HAL resource representation to a specific Json representation.
    /// The interpreter transforms the generic Json representation to a specific representation.
    let toJson interpreter resource =
        resource |> serialize |> interpreter

[<assembly:System.Runtime.CompilerServices.InternalsVisibleTo ("halsharp.tests")>]
()