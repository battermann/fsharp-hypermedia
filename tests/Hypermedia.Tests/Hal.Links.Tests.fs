module Hal.Links.Tests

open Expecto
open Chiron
open System

open Hypermedia
open Hal

let relUri path = Uri(path, UriKind.Relative)

[<Tests>]
let ``Link with ChironInterpreter`` =
  testList "links" [
    testCase "create link to string" <| fun _ ->
      Expect.equal 
        (Link.create (relUri "/orders") |> Link.serializeSingleLink |> ChironInterpreter.interpret|> Json.format) 
        """{"href":"/orders"}""" 
        "should return create link with href attribute"
    testCase "link with template to string" <| fun _ ->
      Expect.equal 
        ({ Link.create (relUri "/orders/{id}") with templated = Some true } |> Link.serializeSingleLink |> ChironInterpreter.interpret|> Json.format) 
        """{"href":"/orders/{id}","templated":true}""" 
        "should return link object with href and templated attribute"
    testCase "link with other attributes" <| fun _ ->
      let link = { 
        Link.create (relUri "/orders/{id}")
        with templated = Some true
             mediaType = Some "application/json"
             deprication = Some (System.Uri("http://example.com/deprications/foo"))
             name = Some "j39fh23hf"
             title = Some "Order"
             profile = Some (System.Uri("http://example.com/profiles/foo"))
             hreflang = Some "de-de"
      }
      Expect.equal 
        (link |> Link.serializeSingleLink |> ChironInterpreter.interpret |> Json.format) 
        """{"deprication":"http://example.com/deprications/foo","href":"/orders/{id}","hreflang":"de-de","name":"j39fh23hf","profile":"http://example.com/profiles/foo","templated":true,"title":"Order","type":"application/json"}""" 
        "should return link object with href and templated attribute"        
  ]