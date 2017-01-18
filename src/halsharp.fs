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
        embedded: Map<string, Resource list>
        properties: Map<string, Json>
    }

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

    let serializeLink link = 
        Object <| Map.ofList
            [ yield ("href", String link.href)
              yield! match link.templated with Some b -> [ ("templated", Bool b) ] | _ -> [] ]

    let serializeLinks links =
        if links |> Map.isEmpty then
            Map.empty
        else
            links
            |> Map.map (fun rel link -> serializeLink link)
            |> Object
            |> fun links -> Map.ofList [ "_links", links ]


module Resource =
    open ResourceDefinition

    let EmptyObject = Object <| Map.empty

    let rec serializeResource resource : Json =
        let merge (maps: Map<'a,'b> seq): Map<'a,'b> = 
            Map.ofList <| List.concat (maps |> Seq.map Map.toList)

        let links = Link.serializeLinks resource.links

        let serializeEmbedded resources =
            match resources |> List.length with
            | 0 -> EmptyObject
            | 1 -> serializeResource (resources |> List.head)
            | _ -> Array (resources |> List.map serializeResource)
            
        let embedded =
            let embeddedMap = 
                resource.embedded
                |> Map.map (fun rel res -> serializeEmbedded res)
                
            if embeddedMap |> Map.isEmpty then
                Map.empty
            else
                Map.ofList [ "_embedded", embeddedMap |> Object ]

        let members = merge [ links; resource.properties; embedded ]
                                     
        if members |> Map.isEmpty then
            EmptyObject
        else
            members |> Object