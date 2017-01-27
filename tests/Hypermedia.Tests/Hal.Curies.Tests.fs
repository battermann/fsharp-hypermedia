module Hal.Curies.Tests

open Expecto
open Chiron
open System

open Hal
open Hypermedia.Models

let relUri path = Uri(path, UriKind.Relative)

[<Tests>]
let ``Curies`` =
  testList "curies" [
    testCase "add curies" <| fun _ ->
      Expect.equal 
        ({ Resource.empty with curies = Map.ofList [ "foo", Uri "http://example.com/docs/rels/{rel}" ] } |> Resource.serialize)
        (JRecord (Map.ofList [ "_links", JRecord (Map.ofList ["curies", JArray [ JRecord (Map.ofList ["name", JString "foo"; "href", JString "http://example.com/docs/rels/{rel}"; "templated", JBool true ])]])]))
        "curies should be added to the links" 
        
    testCase "add curies, replace links recursively" <| fun _ ->
      let order1 = {
        Resource.empty with
          links = Map.ofList [ "self", Singleton (Link.create (relUri "/orders/123"))
                               "http://example.com/docs/rels/basket", Singleton (Link.create (relUri "/baskets/98712"))
                               "http://example.com/docs/rels/customer", Singleton (Link.create (relUri "/customers/7809")) ]
          properties = Map.ofList [ "total", JObject (Number 30M)
                                    "currency", JString "USD"
                                    "status", JString "shipped" ]
      }

      let order2 = {
        Resource.empty with
          links = Map.ofList [ "self", Singleton (Link.create (relUri "/orders/124"))
                               "http://example.com/docs/rels/basket", Singleton (Link.create (relUri "/baskets/97213"))
                               "http://example.com/docs/rels/customer", Singleton (Link.create (relUri "/customers/12369")) ]
          properties = Map.ofList [ "total", JObject (Number 20M)
                                    "currency", JString "USD"
                                    "status", JString "processing" ]
      }

      let resource = {
        Resource.empty with
          links = Map.ofList [ "self", Singleton (Link.create (relUri "/orders"))
                               "next", Singleton (Link.create (relUri "/orders?page=2"))
                               "http://example.com/docs/rels/find", Singleton ({ Link.create (relUri "/orders{?id}") with templated = Some true })
                               "http://example.com/docs/rels/admin", Collection [ { Link.create (relUri "/admins/2") with title = Some "Fred" }
                                                                                  { Link.create (relUri "/admins/5") with title = Some "Kate" } ] ]
          properties = Map.ofList [ "currentlyProcessing", JObject (Number 14M)
                                    "shippedToday", JObject (Number 20M) ]      
          embedded = Map.ofList [ "http://example.com/docs/rels/order", Collection [ order1; order2 ] ]
          curies = Map.ofList [ "ea", Uri "http://example.com/docs/rels/{rel}" ]
      }

      Expect.equal 
        (resource |> ChironInterpreter.toJson|> Json.format)
        """{"_embedded":{"ea:order":[{"_links":{"ea:basket":{"href":"/baskets/98712"},"ea:customer":{"href":"/customers/7809"},"self":{"href":"/orders/123"}},"currency":"USD","status":"shipped","total":30},{"_links":{"ea:basket":{"href":"/baskets/97213"},"ea:customer":{"href":"/customers/12369"},"self":{"href":"/orders/124"}},"currency":"USD","status":"processing","total":20}]},"_links":{"curies":[{"href":"http://example.com/docs/rels/{rel}","name":"ea","templated":true}],"ea:admin":[{"href":"/admins/2","title":"Fred"},{"href":"/admins/5","title":"Kate"}],"ea:find":{"href":"/orders{?id}","templated":true},"next":{"href":"/orders?page=2"},"self":{"href":"/orders"}},"currentlyProcessing":14,"shippedToday":20}"""
        "curies should be added to the links"         
  ]