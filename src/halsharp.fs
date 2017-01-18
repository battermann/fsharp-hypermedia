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
        links: Map<string, Link list>
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
              yield! match link.templated with Some b -> [ "templated", Bool b ] | _ -> []
              yield! match link.mediaType with Some mt -> [ "type", String mt ] | _ -> []
              yield! match link.deprication with Some dep -> [ "deprication", String (dep.ToString()) ] | _ -> []
              yield! match link.name with Some name -> [ "name", String name ] | _ -> []
              yield! match link.profile with Some prof -> [ "profile", String (prof.ToString()) ] | _ -> []
              yield! match link.title with Some title -> [ "title", String title ] | _ -> []
              yield! match link.hreflang with Some lang -> [ "hreflang", String lang ] | _ -> [] ]
    let serializeLinks links =
        let serializeNonEmptyLinkList links =
            match links |> List.length with
            | 1 -> serializeLink (links |> List.head)
            | _ -> Array (links |> List.map serializeLink)

        let nonEmptyLinks = links |> Map.filter (fun _ linkList -> not (List.isEmpty linkList))
        if nonEmptyLinks |> Map.isEmpty then
            Map.empty
        else
            nonEmptyLinks
            |> Map.map (fun rel linkList -> serializeNonEmptyLinkList linkList)
            |> Object
            |> fun links -> Map.ofList [ "_links", links ]


module Resource =
    open ResourceDefinition

    let rec serializeResource resource : Json =
        let merge (maps: Map<'a,'b> seq): Map<'a,'b> = 
            Map.ofList <| List.concat (maps |> Seq.map Map.toList)
          
        let embedded =
            let serializeEmbedded resources =
                match resources |> List.length with
                | 0 -> Object <| Map.empty
                | 1 -> serializeResource (resources |> List.head)
                | _ -> Array (resources |> List.map serializeResource)

            let embeddedMap = 
                resource.embedded
                |> Map.map (fun rel res -> serializeEmbedded res)
                
            if embeddedMap |> Map.isEmpty then
                Map.empty
            else
                Map.ofList [ "_embedded", embeddedMap |> Object ]

        let links = Link.serializeLinks resource.links                

        [ links; resource.properties; embedded ]
        |> merge
        |> Object