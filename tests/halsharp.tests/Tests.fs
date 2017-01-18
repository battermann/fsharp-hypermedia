module Tests

open Expecto
open Chiron

open HalSharp

open ResourceDefinition

open Resource

// todo ensure that _links and _embedded are reserved
// implement curies

[<Tests>]
let ``Empty resource`` =
    testCase "empty resource" <| fun _ ->
      let resource = {
        links = Map.empty
        embedded = Map.empty
        properties = Map.empty
      }
        
      Expect.equal 
        (resource |> serializeResource |> Json.format) 
        """{}""" 
        "should return an empty resource" 

[<Tests>]
let ``Link tests`` =

  testList "links" [
    testCase "simple link to string" <| fun _ ->
      Expect.equal 
        (Link.simpleLink "/orders" |> Link.serializeLink |> Json.format) 
        """{"href":"/orders"}""" 
        "should return simple link with href attribute"
    testCase "link with template to string" <| fun _ ->
      Expect.equal 
        ({ Link.simpleLink "/orders/{id}" with templated = Some true } |> Link.serializeLink |> Json.format) 
        """{"href":"/orders/{id}","templated":true}""" 
        "should return link object with href and templated attribute"
    testCase "link with other attributes" <| fun _ ->
      let link = { 
        Link.simpleLink "/orders/{id}" 
        with templated = Some true
             mediaType = Some "application/json"
             deprication = Some (System.Uri("http://example.com/deprications/foo"))
             name = Some "j39fh23hf"
             title = Some "Order"
             profile = Some (System.Uri("http://example.com/profiles/foo"))
             hreflang = Some "de-de"
      }
      Expect.equal 
        (link |> Link.serializeLink |> Json.format) 
        """{"deprication":"http://example.com/deprications/foo","href":"/orders/{id}","hreflang":"de-de","name":"j39fh23hf","profile":"http://example.com/profiles/foo","templated":true,"title":"Order","type":"application/json"}""" 
        "should return link object with href and templated attribute"        
  ]


[<Tests>]
let ``Resource tests`` =
  let someOject =
      Object <| Map.ofList
          [ "string", String "hello"
            "number", Number 42M
            "json", Object (Map [ "hello", String "world" ]) ]

  testList "resource" [   
    testCase "resource with links to json" <| fun _ ->
      let resource = {
        links = Map.ofList [ "self", [ Link.simpleLink "/orders" ]
                             "next", [ Link.simpleLink "/orders?page=2" ]
                           ]
        embedded = Map.empty
        properties = Map.empty
      }
        
      Expect.equal 
        (resource |> serializeResource |> Json.format) 
        """{"_links":{"next":{"href":"/orders?page=2"},"self":{"href":"/orders"}}}""" 
        "should return resource object with a _links object"   

    testCase "resource with links with empty link list" <| fun _ ->
      let resource = {
        links = Map.ofList [ "self", [ ] ]
        embedded = Map.empty
        properties = Map.empty
      }
        
      Expect.equal 
        (resource |> serializeResource |> Json.format) 
        """{}""" 
        "should return resource object without a _links property"

    testCase "resource with a link with multiple links" <| fun _ ->
      let resource = {
        links = Map.ofList [ "http://booklistapi.com/rels/authors", [ Link.simpleLink "/author/4554"
                                                                      Link.simpleLink "/author/5758"
                                                                      Link.simpleLink "/author/6853" ] ]        
        embedded = Map.empty
        properties = Map.empty
      }
        
      Expect.equal 
        (resource |> serializeResource |> Json.format) 
        """{"_links":{"http://booklistapi.com/rels/authors":[{"href":"/author/4554"},{"href":"/author/5758"},{"href":"/author/6853"}]}}""" 
        "should return resource object a link relation with multiple links"            

    testCase "resource with properties to json" <| fun _ ->
      let resource = {
        links = Map.empty
        embedded = Map.empty
        properties = Map.ofList [ "currentlyProcessing", Number 14M
                                  "shippedToday", Number 20M ]
      }
        
      Expect.equal 
        (resource |> serializeResource |> Json.format) 
        """{"currentlyProcessing":14,"shippedToday":20}""" 
        "should return resource object with two properties"      

    testCase "resource with property with json to json" <| fun _ ->
      let resource = {
        links = Map.empty
        embedded = Map.empty
        properties = Map.ofList [ "thing", someOject ]
      }
        
      Expect.equal 
        (resource |> serializeResource |> Json.format) 
        """{"thing":{"json":{"hello":"world"},"number":42,"string":"hello"}}""" 
        "should return resource object with property with json"  

    testCase "resource with embedded" <| fun _ ->
      let embedded = {
        links = Map.ofList [ "self", [ Link.simpleLink "/orders" ]
                             "next", [ Link.simpleLink "/orders?page=2" ]
                           ]
        embedded = Map.empty
        properties = Map.ofList [ "thing", someOject ]
      }

      let resource = {
        links = Map.empty
        embedded = Map.ofList [ "thing", [ embedded; embedded ] ]
        properties = Map.empty
      }
        
      Expect.equal 
        (resource |> serializeResource |> Json.format) 
        """{"_embedded":{"thing":[{"_links":{"next":{"href":"/orders?page=2"},"self":{"href":"/orders"}},"thing":{"json":{"hello":"world"},"number":42,"string":"hello"}},{"_links":{"next":{"href":"/orders?page=2"},"self":{"href":"/orders"}},"thing":{"json":{"hello":"world"},"number":42,"string":"hello"}}]}}""" 
        "should return resource object with an embedded resource"                                    
  ]