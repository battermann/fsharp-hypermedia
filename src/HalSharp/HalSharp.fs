module HalSharp

open System

type Instance<'a> =
    | Pure of 'a
    | Instance of Map<string, Instance<'a>>
    | String of string
    | Array of Instance<'a> list
    | Bool of bool

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

type Resource<'a> = {
    links: Map<string, Link list>
    embedded: Map<string, Resource<'a> list>
    properties: Map<string, Instance<'a>>
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

    let serializeSingleLink link : Instance<'a> =
        Instance <| Map.ofList
            [ yield ("href", String link.href)
              yield! match link.templated with Some b -> [ "templated", Bool b ] | _ -> []
              yield! match link.mediaType with Some mt -> [ "type", String mt ] | _ -> []
              yield! match link.deprication with Some dep -> [ "deprication", String (dep.ToString()) ] | _ -> []
              yield! match link.name with Some name -> [ "name", String name ] | _ -> []
              yield! match link.profile with Some prof -> [ "profile", String (prof.ToString()) ] | _ -> []
              yield! match link.title with Some title -> [ "title", String title ] | _ -> []
              yield! match link.hreflang with Some lang -> [ "hreflang", String lang ] | _ -> [] ]

    let serialize (links: Map<string, Link list>) : Instance<'a> option =
        let serializeLinkList links =
            match links |> List.length with
            | 1 -> serializeSingleLink (links |> List.head)
            | _ -> Array (links |> List.map serializeSingleLink)

        let nonEmptyLinks = links |> Map.filter (fun _ linkList -> not (List.isEmpty linkList))

        if nonEmptyLinks |> Map.isEmpty then
            None
        else
            nonEmptyLinks
            |> Map.map (fun _ linkList -> serializeLinkList linkList)
            |> Instance
            |> Some

[<RequireQualifiedAccess>]
module Resource =
    let empty = {
        links = Map.empty
        embedded = Map.empty
        properties = Map.empty
    }
    let rec serialize resource : Instance<'a> =
        let merge (maps: Map<_,_> seq): Map<_,_> = 
            Map.ofList <| List.concat (maps |> Seq.map Map.toList)
        
        let embedded =
            let serializeEmbedded resources =
                match resources |> List.length with
                | 0 -> Instance <| Map.empty
                | 1 -> serialize (resources |> List.head)
                | _ -> Array (resources |> List.map serialize)

            let embeddedMap = 
                resource.embedded
                |> Map.map (fun rel res -> serializeEmbedded res)
                
            if embeddedMap |> Map.isEmpty then
                Map.empty
            else
                Map.ofList [ "_embedded", embeddedMap |> Instance ]

        let links = 
            match Link.serialize resource.links with
            | Some ls -> Map.ofList [ "_links", ls ]
            | _       -> Map.empty

        [ links; resource.properties; embedded ]
        |> merge
        |> Instance

[<assembly:System.Runtime.CompilerServices.InternalsVisibleTo ("halsharp.tests")>]
()        