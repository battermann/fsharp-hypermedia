/// Define Hypertext Application Language (HAL) resources.
/// Transform HAL resource to specific Json implementations.
module Hal

open System
open System.Reflection
open Microsoft.FSharp.Reflection

/// Represents a minimal generic Json object to describe a HAL resource.
type AbstractJsonObject<'a> =
    | JObject of 'a
    | JRecord of Map<string, AbstractJsonObject<'a>>
    | JString of string
    | JArray of AbstractJsonObject<'a> list
    | JBool of bool

/// A link representation according to the HAL specification (https://tools.ietf.org/html/draft-kelly-json-hal-08).
type Link = {
    href: Uri
    templated: bool option
    mediaType: string option
    deprication: Uri option
    name: string option
    profile: Uri option
    title: string option
    hreflang: string option
}

type Curies = Map<string, Uri>

type MaybeSingleton<'a> =
    | Singleton of 'a
    | Collection of 'a list

module MaybeSingleton =
    let map f maybeSingleton =
        match maybeSingleton with
        | Singleton x   -> Singleton (f x)
        | Collection xs -> Collection (xs |> List.map f)

/// A resource representation according to the HAL specification (https://tools.ietf.org/html/draft-kelly-json-hal-08).
type Resource<'a> = {
    curies: Curies
    links: Map<string, MaybeSingleton<Link>>
    embedded: Map<string, MaybeSingleton<Resource<'a>>>
    properties: Map<string, AbstractJsonObject<'a>>
    payload: obj option
}

[<RequireQualifiedAccess>]
[<CompilationRepresentation (CompilationRepresentationFlags.ModuleSuffix)>]
module internal Curies =
    let internal tryFindName (curies: Curies) (relation: string): string option =
        let tryCreateUri str =
            match Uri.TryCreate(str, UriKind.Absolute) with
            | true, uri -> Some uri
            | _         -> None

        let matchUriWithTemplate (name: string, template: Uri) (uri: Uri): string option =
            if template.Segments |> Array.length <> (uri.Segments |> Array.length) then 
                None
            else
                Array.zip template.Segments uri.Segments
                |> Array.tryFind (fst >> ((=) "%7Brel%7D"))
                |> Option.map (snd >> sprintf "%s:%s" name)
        
        curies 
        |> Map.toSeq 
        |> Seq.map matchUriWithTemplate 
        |> Seq.choose (fun tryMatch -> tryCreateUri relation |> Option.bind tryMatch)
        |> Seq.tryHead

    let internal replace (curies: Curies) (map: Map<string, MaybeSingleton<_>>) =
        map
        |> Map.toList
        |> List.map (fun (relation, x) ->
            match tryFindName curies relation with
            | Some name -> name, x
            | _         -> relation, x)
        |> Map.ofList    

    let rec replaceRelations curies resource =
        { resource with 
            links = replace curies resource.links
            embedded = (replace curies resource.embedded) |> Map.map (fun _ emb -> emb |> MaybeSingleton.map (replaceRelations curies))
        }

[<RequireQualifiedAccess>]
[<CompilationRepresentation (CompilationRepresentationFlags.ModuleSuffix)>]
module Link =
    let create href = {
        href = href
        templated = None
        mediaType = None
        deprication = None
        name = None
        profile = None
        title = None
        hreflang = None 
    }

    let internal serializeSingleLink (link: Link) : AbstractJsonObject<'a> =
        Map.ofList
            [ yield ("href", JString (string link.href))
              yield! match link.templated with Some b -> [ "templated", JBool b ] | _ -> []
              yield! match link.mediaType with Some mt -> [ "type", JString mt ] | _ -> []
              yield! match link.deprication with Some dep -> [ "deprication", JString (dep.ToString()) ] | _ -> []
              yield! match link.name with Some name -> [ "name", JString name ] | _ -> []
              yield! match link.profile with Some prof -> [ "profile", JString (prof.ToString()) ] | _ -> []
              yield! match link.title with Some title -> [ "title", JString title ] | _ -> []
              yield! match link.hreflang with Some lang -> [ "hreflang", JString lang ] | _ -> [] ]
            |> JRecord

    let internal serialize (links: Map<string, MaybeSingleton<Link>>) (curies: Curies) : AbstractJsonObject<'a> option =

        let linksWithCuries =
            curies 
            |> Map.toList 
            |> List.map (fun (name, href) -> { create href with name = Some name; templated = Some true })
            |> Collection
            |> fun x -> "curies", x
            |> links.Add

        let serializeLinkList = function
            | Singleton l   -> serializeSingleLink l
            | Collection ls -> JArray (ls |> List.map serializeSingleLink)

        let nonEmptyLinks = 
            linksWithCuries 
            |> Map.filter (fun _ l -> 
                match l with 
                | Singleton _     -> true
                | Collection xs -> not (xs |> List.isEmpty))

        if nonEmptyLinks |> Map.isEmpty then
            None
        else
            nonEmptyLinks
            |> Map.map (fun _ l -> serializeLinkList l)
            |> JRecord
            |> Some

/// Contains functions to transform resources to Json representations.
[<RequireQualifiedAccess>]
module Resource =

    /// Returns an empty resource object the represents a valid HAL resource.
    let empty = {
        curies = Map.empty
        links = Map.empty
        embedded = Map.empty
        properties = Map.empty
        payload = None
    }

    let internal tryToMap(x: 'T) =
        let objectToMap(obj: obj) =
            obj.GetType().GetProperties(BindingFlags.DeclaredOnly ||| BindingFlags.Public ||| BindingFlags.Instance) 
            |> Array.map (fun prop -> prop.Name, prop.GetValue(obj, null))
            |> Map.ofArray

        let recordToMap (record:'T) = 
            [ for p in FSharpType.GetRecordFields(typeof<'T>) ->
                p.Name, p.GetValue(record) ]
            |> Map.ofSeq

        try
            if FSharp.Reflection.FSharpType.IsRecord(typeof<'T>) then
                x |> recordToMap
            else
                x |> objectToMap
        with
        | _ -> Map.empty    

    let rec internal serialize (resource: Resource<'a>) : AbstractJsonObject<'a> =
        let merge (maps: Map<_,_> seq): Map<_,_> = 
            List.concat (maps |> Seq.map Map.toList) |> Map.ofList
        
        let withResolvedCuries = Curies.replaceRelations resource.curies resource
            
        let embedded =        
            let embeddedMap = 
                withResolvedCuries.embedded
                |> Map.map (fun rel res -> 
                    match res with
                    | Singleton x   -> serialize x
                    | Collection xs -> JArray (xs |> List.map serialize))
                
            if embeddedMap |> Map.isEmpty then
                Map.empty
            else
                Map.ofList [ "_embedded", embeddedMap |> JRecord ]

        let links = 
            match Link.serialize withResolvedCuries.links resource.curies with
            | Some ls -> Map.ofList [ "_links", ls ]
            | _       -> Map.empty

        let properties =
            match resource.payload with
            | Some pl ->
                let payload =
                    try
                        pl |> tryToMap |> Map.map (fun _ v -> JObject (v :?> 'a))
                    with
                    | _ -> Map.empty
                merge [ resource.properties; payload ]
            | _ -> resource.properties

        [ links; properties; embedded ]
        |> merge
        |> JRecord

    /// Serializes a HAL resource representation to a specific Json representation.
    /// The interpreter transforms the generic Json representation to a specific representation.
    let toJson interpreter resource =
        resource |> serialize |> interpreter

    let withPayload (payload: obj) resource : Resource<obj> =
        { resource with payload = payload |> Some }

    let withLinks links resource =
        { resource with links = Map.ofList links }
    let addLink rel link resource =
        { resource with links = resource.links.Add(rel, Singleton link) }

    let addLinkCollection rel link resource =
        { resource with links = resource.links.Add(rel, Collection link) }             

    let addEmbedded name embedded resource =
        { resource with embedded = resource.embedded.Add(name, Singleton embedded) }

    let addEmbeddedCollection name embedded resource =
        { resource with embedded = resource.embedded.Add(name, Collection embedded) }        

    let withCuries curies resource =
        { resource with curies = Map.ofList curies }

    let withProperties props resource = 
        { resource with properties = props |> Map.ofList |> Map.map (fun _ x -> JObject x) }

    let addProperty name prop resource = 
        { resource with properties = resource.properties.Add(name, JObject prop) }

[<assembly:System.Runtime.CompilerServices.InternalsVisibleTo ("halsharp.tests")>]
()