module HalSharp

open System
open Chiron

module ResourceDefinition =

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

    type Resource = {
        links: Map<string, Link>
        embedded: Embedded
        properties: Map<string, Json>
    }
    and Embedded = Map<string, Resource list>

module Link =
    open ResourceDefinition
    let simpleLink href = {
        href = href
        templated = None
        mediaType = None
        deprication = None
        name = None
        profile = None
        title = None
        hreflang = None 
    }

    let serializeLink link : Json = 
        Object <| Map.ofList
            [ yield ("href", String link.href)
              yield! match link.templated with Some b -> [ ("templated", Bool b) ] | _ -> [] ]

    let serializeLinks links =
        let createLink (rel, link) = rel, serializeLink link

        if links |> Map.isEmpty then
            Map.empty
        else
            links
            |> Map.toList
            |> List.map createLink
            |> Map.ofList |> Object
            |> fun links -> Map.ofList [ "_links", links ]

module Resource =
    open ResourceDefinition

    let EmptyObject = Object <| Map.empty

    let serializeResource resource : Json =
        let merge (p:Map<'a,'b>) (q:Map<'a,'b>) = 
            Map(Seq.concat [ (Map.toSeq p) ; (Map.toSeq q) ])

        let links = Link.serializeLinks resource.links
        
        let members = merge links resource.properties
                                     
        if members |> Map.isEmpty then
            EmptyObject
        else
            members |> Object