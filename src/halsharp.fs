module HalSharp

open System
open Chiron

module Types =

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
        properties: Map<string, Object>
    }
    and Embedded = Map<string, Resource>

module Links =
    open Types
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
        json {
            do! Json.write "href" link.href
            match link.templated with
            | Some b -> do! Json.write "templated" b
            | _      -> ()
        }

    let toJson link = 
        link
        |> Json.serializeWith serializeLink 
        |> Json.formatWith JsonFormattingOptions.Compact        