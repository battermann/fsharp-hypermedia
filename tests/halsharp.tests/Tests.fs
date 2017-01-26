module Tests

open Expecto
open Chiron
open System

open Hal

// todo ensure that _links and _embedded are unique

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
          links = Map.ofList [ "self", Singleton (Link.simple (relUri "/orders/123"))
                               "http://example.com/docs/rels/basket", Singleton (Link.simple (relUri "/baskets/98712"))
                               "http://example.com/docs/rels/customer", Singleton (Link.simple (relUri "/customers/7809")) ]
          properties = Map.ofList [ "total", JObject (Number 30M)
                                    "currency", JString "USD"
                                    "status", JString "shipped" ]
      }

      let order2 = {
        Resource.empty with
          links = Map.ofList [ "self", Singleton (Link.simple (relUri "/orders/124"))
                               "http://example.com/docs/rels/basket", Singleton (Link.simple (relUri "/baskets/97213"))
                               "http://example.com/docs/rels/customer", Singleton (Link.simple (relUri "/customers/12369")) ]
          properties = Map.ofList [ "total", JObject (Number 20M)
                                    "currency", JString "USD"
                                    "status", JString "processing" ]
      }

      let resource = {
        Resource.empty with
          links = Map.ofList [ "self", Singleton (Link.simple (relUri "/orders"))
                               "next", Singleton (Link.simple (relUri "/orders?page=2"))
                               "http://example.com/docs/rels/find", Singleton ({ Link.simple (relUri "/orders{?id}") with templated = Some true })
                               "http://example.com/docs/rels/admin", Collection [ { Link.simple (relUri "/admins/2") with title = Some "Fred" }
                                                                                  { Link.simple (relUri "/admins/5") with title = Some "Kate" } ] ]
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

[<Tests>]
let ``Empty resource with ChironInterpreter`` =
    testCase "empty resource" <| fun _ ->
      Expect.equal 
        (Resource.empty |> Resource.toJson ChironInterpreter.interpret|> Json.format) 
        """{}""" 
        "should return an empty resource" 

[<Tests>]
let ``Link with ChironInterpreter`` =
  testList "links" [
    testCase "simple link to string" <| fun _ ->
      Expect.equal 
        (Link.simple (relUri "/orders") |> Link.serializeSingleLink |> ChironInterpreter.interpret|> Json.format) 
        """{"href":"/orders"}""" 
        "should return simple link with href attribute"
    testCase "link with template to string" <| fun _ ->
      Expect.equal 
        ({ Link.simple (relUri "/orders/{id}") with templated = Some true } |> Link.serializeSingleLink |> ChironInterpreter.interpret|> Json.format) 
        """{"href":"/orders/{id}","templated":true}""" 
        "should return link object with href and templated attribute"
    testCase "link with other attributes" <| fun _ ->
      let link = { 
        Link.simple (relUri "/orders/{id}")
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
      links = Map.ofList [ "self", Singleton <| Link.simple (relUri ("/shipping/135451")) ]
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
      links = Map.ofList [ "self", Singleton (Link.simple (relUri "/billing/135451")) ]
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
      links = Map.ofList [ "self", Singleton <| Link.simple (relUri "/payment")
                           "http://example.com/rels/billing", Singleton <| Link.simple (relUri "/member/109087/billing")
                           "http://example.com/rels/shipping", Singleton <| Link.simple (relUri "/member/109087/shipping")
                           "http://example.com/rels/payment/coupon", Singleton <| Link.simple (relUri "/payment/coupon")
                           "http://example.com/rels/payment/billing", Singleton <| Link.simple (relUri "/payment/billing")
                           "http://example.com/rels/payment/shipping", Singleton <| Link.simple (relUri "/payment/shipping")
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
          links = Map.ofList [ "self", Singleton <| Link.simple (relUri "/orders")
                               "next", Singleton <| Link.simple (relUri "/orders?page=2") ]
      }
      Expect.equal 
        (resource |> ChironInterpreter.toJson |> Json.format) 
        """{"_links":{"next":{"href":"/orders?page=2"},"self":{"href":"/orders"}}}""" 
        "should return resource object with a _links object"   

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
            [ "http://booklistapi.com/rels/authors", Collection [ Link.simple (relUri "/author/4554")
                                                                  Link.simple (relUri "/author/5758")
                                                                  Link.simple (relUri "/author/6853") ] ]
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
          links = Map.ofList [ "self", Singleton <| Link.simple (relUri "/orders")
                               "next", Singleton <| Link.simple (relUri "/orders?page=2")
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

open FSharp.Data

[<Tests>]
let ``Resource with FSharpDataInterpreter`` =
  let coupon = {
    Resource.empty with
      properties = Map.ofList [ "type", JObject <| JsonValue.String("dollarOff")
                                "amount", JObject <| JsonValue.String("10")
                                "code", JObject <| JsonValue.String("A0318A97") ]
  }

  let shipping = {
    Resource.empty with
      links = Map.ofList [ "self", Singleton <| Link.simple (relUri "/shipping/135451") ]
      properties = Map.ofList [ "first_name", JObject <| JsonValue.String("Heman")
                                "last_name", JObject <| JsonValue.String("Radtke")
                                "address", JObject <| JsonValue.String( "1234 Day St.")
                                "city", JObject <| JsonValue.String( "Los Angeles")
                                "state", JObject <| JsonValue.String( "CA")
                                "zipcode", JObject <| JsonValue.String( "90015")
                                "country_iso", JObject <| JsonValue.String( "US") ]        
  }

  let billing = {
    Resource.empty with
      links = Map.ofList [ "self", Singleton <| Link.simple (relUri "/billing/135451") ]
      properties = Map.ofList [ "first_name", JObject <| JsonValue.String("Herman")
                                "last_name", JObject <| JsonValue.String( "Radtke")
                                "address", JObject <| JsonValue.String( "1234 Day St.")
                                "city", JObject <| JsonValue.String( "Los Angeles")
                                "state", JObject <| JsonValue.String( "CA")
                                "zipcode", JObject <| JsonValue.String( "90015")
                                "country_iso", JObject <| JsonValue.String( "US")
                                "card_number", JObject <| JsonValue.String( "1111")
                                "card_type", JObject <| JsonValue.String( "mastercard")
                                "card_exp_year", JObject <| JsonValue.String( "2015")
                                "card_exp_month", JObject <| JsonValue.String( "01") ]        
  }

  let eCommerceResource = {
    Resource.empty with
      links = Map.ofList [ "self", Singleton <| Link.simple (relUri "/payment")
                           "http://example.com/rels/billing", Singleton <| Link.simple (relUri "/member/109087/billing")
                           "http://example.com/rels/shipping", Singleton <| Link.simple (relUri "/member/109087/shipping")
                           "http://example.com/rels/payment/coupon", Singleton <| Link.simple (relUri "/payment/coupon")
                           "http://example.com/rels/payment/billing", Singleton <| Link.simple (relUri "/payment/billing")
                           "http://example.com/rels/payment/shipping", Singleton <| Link.simple (relUri "/payment/shipping")
                        ]
      embedded = Map.ofList [ "http://www.example.com/rels/coupon", Singleton <|  coupon
                              "http://example.com/rels/shipping", Singleton <| shipping
                              "http://example.com/rels/billing", Singleton <| billing ]
      properties = Map.ofList [ "subtotal", JObject <| JsonValue.Number(49M)
                                "tax", JObject <| JsonValue.Number(0M)
                                "freight", JObject <| JsonValue.Number(5M)
                                "total", JObject <| JsonValue.Number(44M) ]
  }
  testCase "E-commerce example" <| fun _ ->
    Expect.equal 
      (eCommerceResource |> FSharpDataIntepreter.toJson |> fun x -> x.ToString(JsonSaveOptions.DisableFormatting)) 
      """{"_embedded":{"http://example.com/rels/billing":{"_links":{"self":{"href":"/billing/135451"}},"address":"1234 Day St.","card_exp_month":"01","card_exp_year":"2015","card_number":"1111","card_type":"mastercard","city":"Los Angeles","country_iso":"US","first_name":"Herman","last_name":"Radtke","state":"CA","zipcode":"90015"},"http://example.com/rels/shipping":{"_links":{"self":{"href":"/shipping/135451"}},"address":"1234 Day St.","city":"Los Angeles","country_iso":"US","first_name":"Heman","last_name":"Radtke","state":"CA","zipcode":"90015"},"http://www.example.com/rels/coupon":{"amount":"10","code":"A0318A97","type":"dollarOff"}},"_links":{"http://example.com/rels/billing":{"href":"/member/109087/billing"},"http://example.com/rels/payment/billing":{"href":"/payment/billing"},"http://example.com/rels/payment/coupon":{"href":"/payment/coupon"},"http://example.com/rels/payment/shipping":{"href":"/payment/shipping"},"http://example.com/rels/shipping":{"href":"/member/109087/shipping"},"self":{"href":"/payment"}},"freight":5,"subtotal":49,"tax":0,"total":44}""" 
      "should return resource object corresponding to correct e-commerce example"

open Newtonsoft.Json

type Person = {
  name: string
  age: int
}

[<Tests>]
let ``Json.NET`` =
  testList "json.net" [
    testCase "empty resource" <| fun _ ->
      Expect.equal 
        (Resource.empty |> ObjectInterpreter.toJson |> JsonConvert.SerializeObject) 
        """{}""" 
        "should return an empty resource"
    testCase "resource" <| fun _ ->
      let coupon : Resource<obj> = {
        Resource.empty with
          properties = Map.ofList [ "type", JObject <| ("dollarOff" :> obj)
                                    "amount", JObject <| ("10" :> obj)
                                    "code", JObject <| ("A0318A97" :> obj) ]
      }

      let shipping = {
        Resource.empty with
          links = Map.ofList [ "self", Singleton <| Link.simple (relUri "/shipping/135451") ]
          properties = Map.ofList [ "first_name", JObject ("Heman" :> obj)
                                    "last_name", JObject ("Radtke" :> obj)
                                    "address", JObject ("1234 Day St." :> obj)
                                    "city", JObject ("Los Angeles" :> obj)
                                    "state", JObject ("CA" :> obj)
                                    "zipcode", JObject ("90015" :> obj)
                                    "country_iso", JObject ("US" :> obj) ]        
      }

      let billing = {
        Resource.empty with
          links = Map.ofList [ "self", Singleton <| Link.simple (relUri "/billing/135451") ]
          properties = Map.ofList [ "first_name", JObject ("Herman" :> obj)
                                    "last_name", JObject ("Radtke" :> obj)
                                    "address", JObject ("1234 Day St." :> obj)
                                    "city", JObject ("Los Angeles" :> obj)
                                    "state", JObject ("CA" :> obj)
                                    "zipcode", JObject ("90015" :> obj)
                                    "country_iso", JObject ("US" :> obj)
                                    "card_number", JObject ("1111" :> obj)
                                    "card_type", JObject ("mastercard" :> obj)
                                    "card_exp_year", JObject ("2015" :> obj)
                                    "card_exp_month", JObject ("01" :> obj) ]        
      }

      let eCommerceResource = {
        Resource.empty with
          links = Map.ofList [ "self", Singleton <| Link.simple (relUri "/payment")
                               "http://example.com/rels/billing", Singleton <| Link.simple (relUri "/member/109087/billing")
                               "http://example.com/rels/shipping", Singleton <|Link.simple (relUri "/member/109087/shipping")
                               "http://example.com/rels/payment/coupon", Singleton <|Link.simple (relUri "/payment/coupon")
                               "http://example.com/rels/payment/billing", Singleton <|Link.simple (relUri "/payment/billing")
                               "http://example.com/rels/payment/shipping", Singleton <|Link.simple (relUri "/payment/shipping")
                            ]
          embedded = Map.ofList [ "http://www.example.com/rels/coupon", Singleton <| coupon
                                  "http://example.com/rels/shipping", Singleton <| shipping
                                  "http://example.com/rels/billing", Singleton <| billing ]
          properties = Map.ofList [ "subtotal", JObject <| (49M :> obj)
                                    "tax", JObject <| (0M :> obj)
                                    "freight", JObject <| (5M :> obj)
                                    "total", JObject <| (44M :> obj) ]
      }    
      Expect.equal 
        (eCommerceResource |> ObjectInterpreter.toJson |> JsonConvert.SerializeObject) 
        """{"_embedded":{"http://example.com/rels/billing":{"_links":{"self":{"href":"/billing/135451"}},"address":"1234 Day St.","card_exp_month":"01","card_exp_year":"2015","card_number":"1111","card_type":"mastercard","city":"Los Angeles","country_iso":"US","first_name":"Herman","last_name":"Radtke","state":"CA","zipcode":"90015"},"http://example.com/rels/shipping":{"_links":{"self":{"href":"/shipping/135451"}},"address":"1234 Day St.","city":"Los Angeles","country_iso":"US","first_name":"Heman","last_name":"Radtke","state":"CA","zipcode":"90015"},"http://www.example.com/rels/coupon":{"amount":"10","code":"A0318A97","type":"dollarOff"}},"_links":{"http://example.com/rels/billing":{"href":"/member/109087/billing"},"http://example.com/rels/payment/billing":{"href":"/payment/billing"},"http://example.com/rels/payment/coupon":{"href":"/payment/coupon"},"http://example.com/rels/payment/shipping":{"href":"/payment/shipping"},"http://example.com/rels/shipping":{"href":"/member/109087/shipping"},"self":{"href":"/payment"}},"freight":5.0,"subtotal":49.0,"tax":0.0,"total":44.0}""" 
        "should return resource object corresponding to correct e-commerce example"
    testCase "resource with payload" <| fun _ ->
      let resource = {
          Resource.empty with
            payload = { name = "John"; age = 42 } :> obj |> Some
        }    
      Expect.equal
        (resource |> ObjectInterpreter.toJson |> JsonConvert.SerializeObject) 
        """{"age":42,"name":"John"}""" 
        "should return an empty resource"
    testCase "resource with embedded with payload" <| fun _ ->
      let person = { name = "Jane"; age = 32 }
      let resource = {
          Resource.empty with
            payload = { name = "John"; age = 42 } :> obj |> Some
        }

      let resourceWithEmbedded = {
        Resource.empty with
          payload = person :> obj |> Some
          embedded = Map.ofList [ "john", Singleton resource ]
      }
      Expect.equal
        (resourceWithEmbedded |> ObjectInterpreter.toJson |> JsonConvert.SerializeObject) 
        """{"_embedded":{"john":{"age":42,"name":"John"}},"age":32,"name":"Jane"}""" 
        "should return an empty resource"                
    testCase "resource with payload and properties" <| fun _ ->
      let resource = {
          Resource.empty with
            properties = Map.ofList [ "total", JObject <| (42M :> obj)
                                      "foo", JObject <| ("bar" :> obj) ]
            payload = { name = "John"; age = 42 } :> obj |> Some
        }    
      Expect.equal
        (resource |> ObjectInterpreter.toJson |> JsonConvert.SerializeObject) 
        """{"age":42,"foo":"bar","name":"John","total":42.0}""" 
        "should return an empty resource"          
    testCase "resource with invalid payload" <| fun _ ->
      let resource = {
          Resource.empty with
            payload = [1;2;3] :> obj |> Some
        }    
      Expect.equal
        (resource |> ObjectInterpreter.toJson |> JsonConvert.SerializeObject) 
        """{}""" 
        "should return an empty resource"                
  ]