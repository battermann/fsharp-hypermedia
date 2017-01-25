module Tests

open Expecto
open Chiron

open Hal
open ChironInterpreter

// todo ensure that _links and _embedded are unique
// implement curies

[<Tests>]
let ``Empty resource with ChironInterpreter`` =
    testCase "empty resource" <| fun _ ->
      Expect.equal 
        (Resource.empty |> Resource.toJson interpret |> Json.format) 
        """{}""" 
        "should return an empty resource" 

[<Tests>]
let ``Link with ChironInterpreter`` =

  testList "links" [
    testCase "simple link to string" <| fun _ ->
      Expect.equal 
        (Link.simple "/orders" |> Link.serializeSingleLink |> interpret |> Json.format) 
        """{"href":"/orders"}""" 
        "should return simple link with href attribute"
    testCase "link with template to string" <| fun _ ->
      Expect.equal 
        ({ Link.simple "/orders/{id}" with templated = Some true } |> Link.serializeSingleLink |> interpret |> Json.format) 
        """{"href":"/orders/{id}","templated":true}""" 
        "should return link object with href and templated attribute"
    testCase "link with other attributes" <| fun _ ->
      let link = { 
        Link.simple "/orders/{id}" 
        with templated = Some true
             mediaType = Some "application/json"
             deprication = Some (System.Uri("http://example.com/deprications/foo"))
             name = Some "j39fh23hf"
             title = Some "Order"
             profile = Some (System.Uri("http://example.com/profiles/foo"))
             hreflang = Some "de-de"
      }
      Expect.equal 
        (link |> Link.serializeSingleLink |> interpret |> Json.format) 
        """{"deprication":"http://example.com/deprications/foo","href":"/orders/{id}","hreflang":"de-de","name":"j39fh23hf","profile":"http://example.com/profiles/foo","templated":true,"title":"Order","type":"application/json"}""" 
        "should return link object with href and templated attribute"        
  ]


[<Tests>]
let ``Resource with ChironInterpreter`` =
  let coupon = {
    links = Map.empty
    embedded = Map.empty
    properties = Map.ofList [ "type", JString "dollarOff"
                              "amount", JString "10"
                              "code", JString "A0318A97" ]
  }

  let shipping = {
    links = Map.ofList [ "self", Singleton <| Link.simple "/shipping/135451" ]
    embedded = Map.empty
    properties = Map.ofList [ "first_name", JString "Heman"
                              "last_name", JString "Radtke"
                              "address", JString "1234 Day St."
                              "city", JString "Los Angeles"
                              "state", JString "CA"
                              "zipcode", JString "90015"
                              "country_iso", JString "US" ]        
  }

  let billing = {
    links = Map.ofList [ "self", Singleton (Link.simple "/billing/135451") ]
    embedded = Map.empty
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
    links = Map.ofList [ "self", Singleton <| Link.simple "/payment" 
                         "http://example.com/rels/billing", Singleton <| Link.simple "/member/109087/billing"
                         "http://example.com/rels/shipping", Singleton <| Link.simple "/member/109087/shipping"
                         "http://example.com/rels/payment/coupon", Singleton <| Link.simple "/payment/coupon"
                         "http://example.com/rels/payment/billing", Singleton <| Link.simple "/payment/billing"
                         "http://example.com/rels/payment/shipping", Singleton <| Link.simple "/payment/shipping"
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
      Object <| Map.ofList
          [ "string", Chiron.String "hello"
            "number", Number 42M
            "json", Object (Map [ "hello", Chiron.String "world" ]) ]

  testList "resource" [   
    testCase "resource with links to json" <| fun _ ->
      let resource = {
        links = Map.ofList [ "self", Singleton <| Link.simple "/orders"
                             "next", Singleton <| Link.simple "/orders?page=2"
                           ]
        embedded = Map.empty
        properties = Map.empty
      }
        
      Expect.equal 
        (resource |> Resource.serialize |> interpret |> Json.format) 
        """{"_links":{"next":{"href":"/orders?page=2"},"self":{"href":"/orders"}}}""" 
        "should return resource object with a _links object"   

    testCase "resource with links with empty link list" <| fun _ ->
      let resource = {
        links = Map.ofList [ "self", Collection [ ] ]
        embedded = Map.empty
        properties = Map.empty
      }
        
      Expect.equal 
        (resource |> Resource.serialize |> interpret |> Json.format) 
        """{}""" 
        "should return resource object without a _links property"

    testCase "resource with a link with multiple links" <| fun _ ->
      let resource = {
        links = Map.ofList 
          [ "http://booklistapi.com/rels/authors", Collection [ Link.simple "/author/4554"
                                                                Link.simple "/author/5758"
                                                                Link.simple "/author/6853" ] ]        
        embedded = Map.empty
        properties = Map.empty
      }
        
      Expect.equal 
        (resource |> Resource.serialize |> interpret |> Json.format) 
        """{"_links":{"http://booklistapi.com/rels/authors":[{"href":"/author/4554"},{"href":"/author/5758"},{"href":"/author/6853"}]}}""" 
        "should return resource object a link relation with multiple links"            

    testCase "resource with properties to json" <| fun _ ->
      let resource = {
        links = Map.empty
        embedded = Map.empty
        properties = Map.ofList [ "currentlyProcessing", JObject (Number 14M)
                                  "shippedToday", JObject (Number 20M) ]
      }
        
      Expect.equal 
        (resource |> Resource.serialize |> interpret |> Json.format) 
        """{"currentlyProcessing":14,"shippedToday":20}""" 
        "should return resource object with two properties"      

    testCase "resource with property with json to json" <| fun _ ->
      let resource = {
        links = Map.empty
        embedded = Map.empty
        properties = Map.ofList [ "thing", JObject someOject ]
      }
        
      Expect.equal 
        (resource |> Resource.serialize |> interpret |> Json.format) 
        """{"thing":{"json":{"hello":"world"},"number":42,"string":"hello"}}""" 
        "should return resource object with property with json"  

    testCase "resource with embedded" <| fun _ ->
      let embedded = {
        links = Map.ofList [ "self", Singleton <| Link.simple "/orders"
                             "next", Singleton <| Link.simple "/orders?page=2"
                           ]
        embedded = Map.empty
        properties = Map.ofList [ "thing", JObject someOject ]
      }

      let resource = {
        links = Map.empty
        embedded = Map.ofList [ "thing", Collection [ embedded; embedded ] ]
        properties = Map.empty
      }
        
      Expect.equal 
        (resource |> Resource.serialize |> interpret |> Json.format) 
        """{"_embedded":{"thing":[{"_links":{"next":{"href":"/orders?page=2"},"self":{"href":"/orders"}},"thing":{"json":{"hello":"world"},"number":42,"string":"hello"}},{"_links":{"next":{"href":"/orders?page=2"},"self":{"href":"/orders"}},"thing":{"json":{"hello":"world"},"number":42,"string":"hello"}}]}}""" 
        "should return resource object with an embedded resource"      

    testCase "E-commerce example" <| fun _ ->       
      Expect.equal 
        (eCommerceResource |> Resource.toJson interpret |> Json.format) 
        """{"_embedded":{"http://example.com/rels/billing":{"_links":{"self":{"href":"/billing/135451"}},"address":"1234 Day St.","card_exp_month":"01","card_exp_year":"2015","card_number":"1111","card_type":"mastercard","city":"Los Angeles","country_iso":"US","first_name":"Herman","last_name":"Radtke","state":"CA","zipcode":"90015"},"http://example.com/rels/shipping":{"_links":{"self":{"href":"/shipping/135451"}},"address":"1234 Day St.","city":"Los Angeles","country_iso":"US","first_name":"Heman","last_name":"Radtke","state":"CA","zipcode":"90015"},"http://www.example.com/rels/coupon":{"amount":"10","code":"A0318A97","type":"dollarOff"}},"_links":{"http://example.com/rels/billing":{"href":"/member/109087/billing"},"http://example.com/rels/payment/billing":{"href":"/payment/billing"},"http://example.com/rels/payment/coupon":{"href":"/payment/coupon"},"http://example.com/rels/payment/shipping":{"href":"/payment/shipping"},"http://example.com/rels/shipping":{"href":"/member/109087/shipping"},"self":{"href":"/payment"}},"freight":5,"subtotal":49,"tax":0,"total":44}""" 
        "should return resource object corresponding to correct e-commerce example"                                                     
  ]

open FSharp.Data

[<Tests>]
let ``Resource with FSharpDataInterpreter`` =
  let coupon = {
    links = Map.empty
    embedded = Map.empty
    properties = Map.ofList [ "type", JObject <| JsonValue.String("dollarOff")
                              "amount", JObject <| JsonValue.String("10")
                              "code", JObject <| JsonValue.String("A0318A97") ]
  }

  let shipping = {
    links = Map.ofList [ "self", Singleton <| Link.simple "/shipping/135451" ]
    embedded = Map.empty
    properties = Map.ofList [ "first_name", JObject <| JsonValue.String("Heman")
                              "last_name", JObject <| JsonValue.String("Radtke")
                              "address", JObject <| JsonValue.String( "1234 Day St.")
                              "city", JObject <| JsonValue.String( "Los Angeles")
                              "state", JObject <| JsonValue.String( "CA")
                              "zipcode", JObject <| JsonValue.String( "90015")
                              "country_iso", JObject <| JsonValue.String( "US") ]        
  }

  let billing = {
    links = Map.ofList [ "self", Singleton <| Link.simple "/billing/135451" ]
    embedded = Map.empty
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
    links = Map.ofList [ "self", Singleton <| Link.simple "/payment"
                         "http://example.com/rels/billing", Singleton <| Link.simple "/member/109087/billing"
                         "http://example.com/rels/shipping", Singleton <|Link.simple "/member/109087/shipping"
                         "http://example.com/rels/payment/coupon", Singleton <|Link.simple "/payment/coupon"
                         "http://example.com/rels/payment/billing", Singleton <|Link.simple "/payment/billing"
                         "http://example.com/rels/payment/shipping", Singleton <|Link.simple "/payment/shipping"
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
      (eCommerceResource |> Resource.serialize |> FSharpDataIntepreter.interpret |> fun x -> x.ToString(JsonSaveOptions.DisableFormatting)) 
      """{"_embedded":{"http://example.com/rels/billing":{"_links":{"self":{"href":"/billing/135451"}},"address":"1234 Day St.","card_exp_month":"01","card_exp_year":"2015","card_number":"1111","card_type":"mastercard","city":"Los Angeles","country_iso":"US","first_name":"Herman","last_name":"Radtke","state":"CA","zipcode":"90015"},"http://example.com/rels/shipping":{"_links":{"self":{"href":"/shipping/135451"}},"address":"1234 Day St.","city":"Los Angeles","country_iso":"US","first_name":"Heman","last_name":"Radtke","state":"CA","zipcode":"90015"},"http://www.example.com/rels/coupon":{"amount":"10","code":"A0318A97","type":"dollarOff"}},"_links":{"http://example.com/rels/billing":{"href":"/member/109087/billing"},"http://example.com/rels/payment/billing":{"href":"/payment/billing"},"http://example.com/rels/payment/coupon":{"href":"/payment/coupon"},"http://example.com/rels/payment/shipping":{"href":"/payment/shipping"},"http://example.com/rels/shipping":{"href":"/member/109087/shipping"},"self":{"href":"/payment"}},"freight":5,"subtotal":49,"tax":0,"total":44}""" 
      "should return resource object corresponding to correct e-commerce example"