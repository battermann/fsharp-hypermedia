module Siren

open System

open Hypermedia.Models

type Rel = Rel of string
type Title = Title of string
type Class = Class of string
type MediaType = MediaType of string
type Href = Href of Uri
type Name = Name of string
type Value = Value of string

type HttpMethod = GET | PUT | POST | DELETE | PATCH

type InputType = 
    | Hidden | Text | Search | Tel | Url | Email | Password
    | Datetime | Date | Month | Week | Time | DatetimeLocal | Number
    | Range | Color | Checkbox | Radio | File

type Field = {
    classes: Class list
    inputTpye: InputType option
    value: Value option
    title: Title option
}

type Action = {
    classes: Class list
    method: HttpMethod option
    href: Href
    title: Title option
    mediaType: MediaType option
    fields: Map<Name, Field>
}

type Link = {
    href: Href
    rel: Rel * Rel list
    classes: Class list
    title: Title option
    mediaType: MediaType option
}

type EmbeddedLink = {
    classes: Class list
    rel: Rel * Rel list
    href: Href
    mediaType: MediaType option
    title: Title option
}

type Entity<'a> = {
    properties: Map<string, AbstractJsonObject<'a>>
    entities: SubEntity<'a> list
    actions: Map<Name, Action>
    links: Link list
    classes: Class list
    title: Title option
}

and SubEntity<'a> =
    | EmbeddedRepresentation of Entity<'a> * Rel
    | EmbeddeLink of EmbeddedLink
