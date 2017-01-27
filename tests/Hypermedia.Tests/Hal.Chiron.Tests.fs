module Hal.Chiron.Tests

open Expecto
open Chiron
open System

open Hal
open Hypermedia.Models

let relUri path = Uri(path, UriKind.Relative)

[<Tests>]
let ``Resource with ChironInterpreter`` =
  let coupon = { 
      Resource.empty with 
        properties = Map.ofList [ "type", JString "dollarOff"
                                  "amount", JString "10"
                                  "code", JString "A0318A97" ]
  }

  let shipping = {
    Resource.empty with
      links = Map.ofList [ "self", Singleton <| Link.create (relUri ("/shipping/135451")) ]
      properties = Map.ofList [ "first_name", JString "Heman"
                                "last_name", JString "Radtke"
                                "address", JString "1234 Day St."
                                "city", JString "Los Angeles"
                                "state", JString "CA"
                                "zipcode", JString "90015"
                                "country_iso", JString "US" ]        
  }

  let billing = {
    Resource.empty with
      links = Map.ofList [ "self", Singleton (Link.create (relUri "/billing/135451")) ]
      properties = Map.ofList [ "first_name", JString "Herman"
                                "last_name", JString "Radtke"
                                "address", JString "1234 Day St."
                                "city", JString "Los Angeles"
                                "state", JString "CA"
                                "zipcode", JString "90015"
                                "country_iso", JString "US"
                                "card_number", JString "1111"
                                "card_type", JString "mastercard"
                                "card_exp_year", JString "2015"
                                "card_exp_month", JString "01" ]        
  }

  let eCommerceResource = {
    Resource.empty with
      links = Map.ofList [ "self", Singleton <| Link.create (relUri "/payment")
                           "http://example.com/rels/billing", Singleton <| Link.create (relUri "/member/109087/billing")
                           "http://example.com/rels/shipping", Singleton <| Link.create (relUri "/member/109087/shipping")
                           "http://example.com/rels/payment/coupon", Singleton <| Link.create (relUri "/payment/coupon")
                           "http://example.com/rels/payment/billing", Singleton <| Link.create (relUri "/payment/billing")
                           "http://example.com/rels/payment/shipping", Singleton <| Link.create (relUri "/payment/shipping")
                        ]
      embedded = Map.ofList [ "http://www.example.com/rels/coupon", Singleton <| coupon
                              "http://example.com/rels/shipping", Singleton <| shipping
                              "http://example.com/rels/billing", Singleton <| billing ]
      properties = Map.ofList [ "subtotal", JObject (Number 49M)
                                "tax", JObject (Number 0M)
                                "freight", JObject (Number 5M)
                                "total", JObject (Number 44M) ]
  }

  let someOject =
      Chiron.Object <| Map.ofList
          [ "string", Chiron.String "hello"
            "number", Number 42M
            "json", Chiron.Object (Map [ "hello", Chiron.String "world" ]) ]

  testList "resource" [   
    testCase "resource with links to json" <| fun _ ->
      let resource = {
        Resource.empty with
          links = Map.ofList [ "self", Singleton <| Link.create (relUri "/orders")
                               "next", Singleton <| Link.create (relUri "/orders?page=2") ]
      }
      Expect.equal 
        (resource |> ChironInterpreter.toJson |> Json.format) 
        """{"_links":{"next":{"href":"/orders?page=2"},"self":{"href":"/orders"}}}""" 
        "should return resource object with a _links object"   

    testCase "empty resource" <| fun _ ->
      Expect.equal 
        (Resource.empty |> Resource.toJson ChironInterpreter.interpret|> Json.format) 
        """{}""" 
        "should return an empty resource"         

    testCase "resource with links with empty link list" <| fun _ ->
      let resource = {
        Resource.empty with
          links = Map.ofList [ "self", Collection [ ] ]
      }
        
      Expect.equal 
        (resource |> ChironInterpreter.toJson|> Json.format) 
        """{}""" 
        "should return resource object without a _links property"

    testCase "resource with a link with multiple links" <| fun _ ->
      let resource = {
        Resource.empty with
          links = Map.ofList 
            [ "http://booklistapi.com/rels/authors", Collection [ Link.create (relUri "/author/4554")
                                                                  Link.create (relUri "/author/5758")
                                                                  Link.create (relUri "/author/6853") ] ]
      }
        
      Expect.equal 
        (resource |> ChironInterpreter.toJson|> Json.format) 
        """{"_links":{"http://booklistapi.com/rels/authors":[{"href":"/author/4554"},{"href":"/author/5758"},{"href":"/author/6853"}]}}""" 
        "should return resource object a link relation with multiple links"            

    testCase "resource with properties to json" <| fun _ ->
      let resource = {
        Resource.empty with
          properties = Map.ofList [ "currentlyProcessing", JObject (Number 14M)
                                    "shippedToday", JObject (Number 20M) ]
      }
        
      Expect.equal 
        (resource |> ChironInterpreter.toJson|> Json.format) 
        """{"currentlyProcessing":14,"shippedToday":20}""" 
        "should return resource object with two properties"      

    testCase "ignore payload" <| fun _ ->
      let resource = {
        Resource.empty with
          properties = Map.ofList [ "currentlyProcessing", JObject (Number 14M)
                                    "shippedToday", JObject (Number 20M) ]        
          payload = JRecord (Map.ofList [ "fourteen", JObject (Number 14M) ]) :> obj |> Some
      }
        
      Expect.equal 
        (resource |> ChironInterpreter.toJson |> Json.format) 
        """{"currentlyProcessing":14,"shippedToday":20}""" 
        "should return resource object with two properties"           

    testCase "resource with invalid payload" <| fun _ ->
      let resource = {
        Resource.empty with
          properties = Map.ofList [ "currentlyProcessing", JObject (Number 14M)
                                    "shippedToday", JObject (Number 20M) ]        
          payload = JArray [ JObject (Number 14M) ] :> obj |> Some
      }
        
      Expect.equal 
        (resource |> ChironInterpreter.toJson|> Json.format) 
        """{"currentlyProcessing":14,"shippedToday":20}""" 
        "should return resource object with two properties"             

    testCase "resource with property with json to json" <| fun _ ->
      let resource = {
        Resource.empty with
          properties = Map.ofList [ "thing", JObject someOject ]
      }
        
      Expect.equal 
        (resource |> ChironInterpreter.toJson|> Json.format) 
        """{"thing":{"json":{"hello":"world"},"number":42,"string":"hello"}}""" 
        "should return resource object with property with json"  

    testCase "resource with embedded" <| fun _ ->
      let embedded = {
        Resource.empty with
          links = Map.ofList [ "self", Singleton <| Link.create (relUri "/orders")
                               "next", Singleton <| Link.create (relUri "/orders?page=2")
                            ]
          properties = Map.ofList [ "thing", JObject someOject ]
      }

      let resource = {
        Resource.empty with
          embedded = Map.ofList [ "thing", Collection [ embedded; embedded ] ]
      }
        
      Expect.equal 
        (resource |> ChironInterpreter.toJson|> Json.format) 
        """{"_embedded":{"thing":[{"_links":{"next":{"href":"/orders?page=2"},"self":{"href":"/orders"}},"thing":{"json":{"hello":"world"},"number":42,"string":"hello"}},{"_links":{"next":{"href":"/orders?page=2"},"self":{"href":"/orders"}},"thing":{"json":{"hello":"world"},"number":42,"string":"hello"}}]}}""" 
        "should return resource object with an embedded resource"      

    testCase "E-commerce example" <| fun _ ->       
      Expect.equal 
        (eCommerceResource |> Resource.toJson ChironInterpreter.interpret|> Json.format) 
        """{"_embedded":{"http://example.com/rels/billing":{"_links":{"self":{"href":"/billing/135451"}},"address":"1234 Day St.","card_exp_month":"01","card_exp_year":"2015","card_number":"1111","card_type":"mastercard","city":"Los Angeles","country_iso":"US","first_name":"Herman","last_name":"Radtke","state":"CA","zipcode":"90015"},"http://example.com/rels/shipping":{"_links":{"self":{"href":"/shipping/135451"}},"address":"1234 Day St.","city":"Los Angeles","country_iso":"US","first_name":"Heman","last_name":"Radtke","state":"CA","zipcode":"90015"},"http://www.example.com/rels/coupon":{"amount":"10","code":"A0318A97","type":"dollarOff"}},"_links":{"http://example.com/rels/billing":{"href":"/member/109087/billing"},"http://example.com/rels/payment/billing":{"href":"/payment/billing"},"http://example.com/rels/payment/coupon":{"href":"/payment/coupon"},"http://example.com/rels/payment/shipping":{"href":"/payment/shipping"},"http://example.com/rels/shipping":{"href":"/member/109087/shipping"},"self":{"href":"/payment"}},"freight":5,"subtotal":49,"tax":0,"total":44}""" 
        "should return resource object corresponding to correct e-commerce example"                                                     
  ]