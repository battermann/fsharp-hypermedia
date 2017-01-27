module SirenSerialization

open Hypermedia.Models
open Hypermedia.Shared
open Siren

let toMap name items =
    if items |> List.isEmpty then
        Map.empty
    else
        Map.ofList [ name, JArray items ]

[<AutoOpen>]
module internal Attributes =
    let CLASS = "class"
    let HREF = "href"
    let TITLE = "title"
    let TYPE = "type"
    let FIELDS = "fields"
    let NAME = "name"
    let REL = "rel"
    let LINKS = "links"
    let VALUE = "value"
    let METHOD = "method"
    let ACTIONS = "actions"
    let ENTITIES = "entities"

[<RequireQualifiedAccess>]
[<CompilationRepresentation (CompilationRepresentationFlags.ModuleSuffix)>]
module internal InputType =
    let serialize inputType =
        match inputType with
        | Hidden -> "hidden"
        | Text -> "text"
        | Search -> "search"
        | Tel -> "tel"
        | Url -> "url"
        | Email -> "email"
        | Password -> "password"
        | Datetime -> "datetime"
        | Date -> "date"
        | Month -> "month"
        | Week -> "week"
        | Time -> "time"
        | DatetimeLocal -> "datetime-local"
        | Number-> "number"
        | Range -> "range"
        | Color -> "color"
        | Checkbox -> "checkbox"
        | Radio -> "radio"
        | File-> "file"

[<RequireQualifiedAccess>]
[<CompilationRepresentation (CompilationRepresentationFlags.ModuleSuffix)>]
module internal HttpMethod =
    let serialize httpMethod =
        match httpMethod with
        | GET -> "GET"
        | PUT -> "PUT"
        | POST -> "POST"
        | DELETE -> "DELETE"
        | PATCH -> "PATCH"

[<RequireQualifiedAccess>]
[<CompilationRepresentation (CompilationRepresentationFlags.ModuleSuffix)>]
module internal Field =
    let empty : Field = {
        classes = List.empty
        inputType = None
        value = None
        title = None
    }
    let serialize name (field: Field) =
        field.classes |> List.map (fun (Class c) -> JString c) |> toMap CLASS
        |> fun map ->
            match field.inputType with Some t -> t | _ -> InputType.Text
            |> InputType.serialize
            |> fun x -> TYPE, JString x
            |> map.Add
        |> fun map -> 
            match field.value with Some (Value v) -> map.Add(VALUE, JString v) | _ -> map
        |> fun map ->
            match field.title with Some (Title t) -> map.Add(TITLE, JString t) | _ -> map
        |> fun map -> map.Add(NAME, JString name)
        |> JRecord


[<RequireQualifiedAccess>]
[<CompilationRepresentation (CompilationRepresentationFlags.ModuleSuffix)>]
module internal Action =
    let create href : Action = {
        classes = List.empty
        httpMethod = None
        href = href
        title = None
        mediaType = None
        fields = Map.empty
    }

    let serialize (action: Action) =
        action.classes |> List.map (fun (Class c) -> JString c) |> toMap CLASS
        |> fun map ->
            match action.httpMethod with Some m -> m | _ -> HttpMethod.GET
            |> HttpMethod.serialize
            |> fun x -> METHOD, JString x
            |> map.Add
        |> fun map -> 
            let (Href href) = action.href
            map.Add(HREF, JString (href.ToString()))
        |> fun map ->
            match action.title with Some (Title t) -> map.Add(TITLE, JString t) | _ -> map
        |> fun map ->
            match action.mediaType with Some (MediaType mt) -> mt | _ -> "application/x-www-form-urlencoded"
            |> fun x -> TYPE, JString x
            |> map.Add
        |> fun map ->
            action.fields |> Map.toList |>  List.map (fun (Name n, f) -> f |> Field.serialize n) |> toMap FIELDS
        |> JRecord

[<RequireQualifiedAccess>]
[<CompilationRepresentation (CompilationRepresentationFlags.ModuleSuffix)>]
module internal Link =

    let create rel href : Link = {
        href = href
        rel = rel, []
        classes = List.empty
        title = None
        mediaType = None
    }

    let serialize (link: Link) =
        link.classes |> List.map (fun (Class c) -> JString c) |> toMap CLASS
        |> fun map -> 
            let (Href href) = link.href
            map.Add(HREF, JString (href.ToString()))
        |> fun map ->
            match link.title with Some (Title t) -> map.Add(TITLE, JString t) | _ -> map
        |> fun map ->
            match link.mediaType with Some (MediaType mt) -> map.Add(TYPE, JString mt) | _ -> map
        |> fun map ->
            fst link.rel :: snd link.rel |> List.map (fun (Rel rel) -> JString rel) |> toMap REL
        |> JRecord

[<RequireQualifiedAccess>]
[<CompilationRepresentation (CompilationRepresentationFlags.ModuleSuffix)>]
module internal Entity =

    let empty : Entity<'a> = {
        properties = Map.empty
        entities = List.empty
        actions = Map.empty
        links = List.empty
        classes = List.empty
        title = None
    }

    let rec serializeRec (rel: Rel option) (entity: Entity<'a>) =
        entity.classes |> List.map (fun (Class c) -> JString c) |> toMap CLASS
        |> fun map ->
            match entity.title with Some (Title t) -> map.Add(TITLE, JString t) | _ -> map
        |> fun map ->
            match rel with Some (Rel r) -> map.Add(REL, JString r) | _ -> map            
        |> fun map ->
            entity.links |> List.map Link.serialize |> toMap LINKS
        |> fun map ->
            [ ACTIONS, entity.actions |> Map.toList |> List.map (fun (Name n, v) -> (n, Action.serialize v)) |> Map.ofList |> JRecord] |> Map.ofList
        |> fun map ->
            merge [ entity.properties; map ]
        |> fun map ->
            let embedded = 
                entity.entities 
                |> List.map 
                    (function
                    | EmbeddedRepresentation (e,r) -> serializeRec (Some r) e
                    | EmbeddedLink link            -> link |> Link.serialize)
                |> toMap ENTITIES
            merge [ embedded; map ]
        |> JRecord

    let serialize entity = serializeRec None entity

    let toJson interpreter entity =
        entity |> serialize |> interpreter        
