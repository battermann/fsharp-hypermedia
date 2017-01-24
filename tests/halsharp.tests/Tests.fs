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
    properties = Map.ofList [ "type", String "dollarOff"
                              "amount", String "10"
                              "code", String "A0318A97" ]
  }

  let shipping = {
    links = Map.ofList [ "self", [ Link.simple "/shipping/135451" ] ]
    embedded = Map.empty
    properties = Map.ofList [ "first_name", String "Heman"
                              "last_name", String "Radtke"
                              "address", String "1234 Day St."
                              "city", String "Los Angeles"
                              "state", String "CA"
                              "zipcode", String "90015"
                              "country_iso", String "US" ]        
  }

  let billing = {
    links = Map.ofList [ "self", [ Link.simple "/billing/135451" ] ]
    embedded = Map.empty
    properties = Map.ofList [ "first_name", String "Herman"
                              "last_name", String "Radtke"
                              "address", String "1234 Day St."
                              "city", String "Los Angeles"
                              "state", String "CA"
                              "zipcode", String "90015"
                              "country_iso", String "US"
                              "card_number", String "1111"
                              "card_type", String "mastercard"
                              "card_exp_year", String "2015"
                              "card_exp_month", String "01" ]        
  }

  let eCommerceResource = {
    links = Map.ofList [ "self", [ Link.simple "/payment" ]
                         "http://example.com/rels/billing", [ Link.simple "/member/109087/billing" ]
                         "http://example.com/rels/shipping", [ Link.simple "/member/109087/shipping" ]
                         "http://example.com/rels/payment/coupon", [ Link.simple "/payment/coupon" ]
                         "http://example.com/rels/payment/billing", [ Link.simple "/payment/billing" ]
                         "http://example.com/rels/payment/shipping", [ Link.simple "/payment/shipping" ]
                       ]
    embedded = Map.ofList [ "http://www.example.com/rels/coupon", [ coupon ]
                            "http://example.com/rels/shipping", [ shipping ]
                            "http://example.com/rels/billing", [ billing ] ]
    properties = Map.ofList [ "subtotal", Pure (Number 49M)
                              "tax", Pure (Number 0M)
                              "freight", Pure (Number 5M)
                              "total", Pure (Number 44M) ]
  }

  let someOject =
      Object <| Map.ofList
          [ "string", Chiron.String "hello"
            "number", Number 42M
            "json", Object (Map [ "hello", Chiron.String "world" ]) ]

  testList "resource" [   
    testCase "resource with links to json" <| fun _ ->
      let resource = {
        links = Map.ofList [ "self", [ Link.simple "/orders" ]
                             "next", [ Link.simple "/orders?page=2" ]
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
        links = Map.ofList [ "self", [ ] ]
        embedded = Map.empty
        properties = Map.empty
      }
        
      Expect.equal 
        (resource |> Resource.serialize |> interpret |> Json.format) 
        """{}""" 
        "should return resource object without a _links property"

    testCase "resource with a link with multiple links" <| fun _ ->
      let resource = {
        links = Map.ofList [ "http://booklistapi.com/rels/authors", [ Link.simple "/author/4554"
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
        properties = Map.ofList [ "currentlyProcessing", Pure (Number 14M)
                                  "shippedToday", Pure (Number 20M) ]
      }
        
      Expect.equal 
        (resource |> Resource.serialize |> interpret |> Json.format) 
        """{"currentlyProcessing":14,"shippedToday":20}""" 
        "should return resource object with two properties"      

    testCase "resource with property with json to json" <| fun _ ->
      let resource = {
        links = Map.empty
        embedded = Map.empty
        properties = Map.ofList [ "thing", Pure someOject ]
      }
        
      Expect.equal 
        (resource |> Resource.serialize |> interpret |> Json.format) 
        """{"thing":{"json":{"hello":"world"},"number":42,"string":"hello"}}""" 
        "should return resource object with property with json"  

    testCase "resource with embedded" <| fun _ ->
      let embedded = {
        links = Map.ofList [ "self", [ Link.simple "/orders" ]
                             "next", [ Link.simple "/orders?page=2" ]
                           ]
        embedded = Map.empty
        properties = Map.ofList [ "thing", Pure someOject ]
      }

      let resource = {
        links = Map.empty
        embedded = Map.ofList [ "thing", [ embedded; embedded ] ]
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
    properties = Map.ofList [ "type", Pure <| JsonValue.String("dollarOff")
                              "amount", Pure <| JsonValue.String("10")
                              "code", Pure <| JsonValue.String("A0318A97") ]
  }

  let shipping = {
    links = Map.ofList [ "self", [ Link.simple "/shipping/135451" ] ]
    embedded = Map.empty
    properties = Map.ofList [ "first_name", Pure <| JsonValue.String("Heman")
                              "last_name", Pure <| JsonValue.String("Radtke")
                              "address", Pure <| JsonValue.String( "1234 Day St.")
                              "city", Pure <| JsonValue.String( "Los Angeles")
                              "state", Pure <| JsonValue.String( "CA")
                              "zipcode", Pure <| JsonValue.String( "90015")
                              "country_iso", Pure <| JsonValue.String( "US") ]        
  }

  let billing = {
    links = Map.ofList [ "self", [ Link.simple "/billing/135451" ] ]
    embedded = Map.empty
    properties = Map.ofList [ "first_name", Pure <| JsonValue.String("Herman")
                              "last_name", Pure <| JsonValue.String( "Radtke")
                              "address", Pure <| JsonValue.String( "1234 Day St.")
                              "city", Pure <| JsonValue.String( "Los Angeles")
                              "state", Pure <| JsonValue.String( "CA")
                              "zipcode", Pure <| JsonValue.String( "90015")
                              "country_iso", Pure <| JsonValue.String( "US")
                              "card_number", Pure <| JsonValue.String( "1111")
                              "card_type", Pure <| JsonValue.String( "mastercard")
                              "card_exp_year", Pure <| JsonValue.String( "2015")
                              "card_exp_month", Pure <| JsonValue.String( "01") ]        
  }

  let eCommerceResource = {
    links = Map.ofList [ "self", [ Link.simple "/payment" ]
                         "http://example.com/rels/billing", [ Link.simple "/member/109087/billing" ]
                         "http://example.com/rels/shipping", [ Link.simple "/member/109087/shipping" ]
                         "http://example.com/rels/payment/coupon", [ Link.simple "/payment/coupon" ]
                         "http://example.com/rels/payment/billing", [ Link.simple "/payment/billing" ]
                         "http://example.com/rels/payment/shipping", [ Link.simple "/payment/shipping" ]
                       ]
    embedded = Map.ofList [ "http://www.example.com/rels/coupon", [ coupon ]
                            "http://example.com/rels/shipping", [ shipping ]
                            "http://example.com/rels/billing", [ billing ] ]
    properties = Map.ofList [ "subtotal", Pure <| JsonValue.Number(49M)
                              "tax", Pure <| JsonValue.Number(0M)
                              "freight", Pure <| JsonValue.Number(5M)
                              "total", Pure <| JsonValue.Number(44M) ]
  }
  testCase "E-commerce example" <| fun _ ->
    Expect.equal 
      (eCommerceResource |> Resource.serialize |> FSharpDataIntepreter.interpret |> fun x -> x.ToString(JsonSaveOptions.DisableFormatting)) 
      """{"_embedded":{"http://example.com/rels/billing":{"_links":{"self":{"href":"/billing/135451"}},"address":"1234 Day St.","card_exp_month":"01","card_exp_year":"2015","card_number":"1111","card_type":"mastercard","city":"Los Angeles","country_iso":"US","first_name":"Herman","last_name":"Radtke","state":"CA","zipcode":"90015"},"http://example.com/rels/shipping":{"_links":{"self":{"href":"/shipping/135451"}},"address":"1234 Day St.","city":"Los Angeles","country_iso":"US","first_name":"Heman","last_name":"Radtke","state":"CA","zipcode":"90015"},"http://www.example.com/rels/coupon":{"amount":"10","code":"A0318A97","type":"dollarOff"}},"_links":{"http://example.com/rels/billing":{"href":"/member/109087/billing"},"http://example.com/rels/payment/billing":{"href":"/payment/billing"},"http://example.com/rels/payment/coupon":{"href":"/payment/coupon"},"http://example.com/rels/payment/shipping":{"href":"/payment/shipping"},"http://example.com/rels/shipping":{"href":"/member/109087/shipping"},"self":{"href":"/payment"}},"freight":5,"subtotal":49,"tax":0,"total":44}""" 
      "should return resource object corresponding to correct e-commerce example"