module Tests

open Expecto
open Chiron

open HalSharp

open ResourceDefinition

open Resource

// todo ensure that _links and _embedded are reserved
// todo implement other link attributes
// allow array of links for a single name
// implement curies

[<Tests>]
let ``Empty resource`` =
    testCase "empty resource" <| fun _ ->
      let resource = {
        links = Map.empty
        embedded = Embedded []
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
  ]


[<Tests>]
let ``Resource tests`` =
  let someOject =
      Object <| Map.ofList
          [ "string", String "hello"
            "number", Number 42M
            "json", Object (Map [ "hello", String "world" ]) ]

  testList "resources" [   
    testCase "resource with links to json" <| fun _ ->
      let resource = {
        links = Map.ofList [ "self", Link.simpleLink "/orders"
                             "next", Link.simpleLink "/orders?page=2"
                           ]
        embedded = Embedded []
        properties = Map.empty
      }
        
      Expect.equal 
        (resource |> serializeResource |> Json.format) 
        """{"_links":{"next":{"href":"/orders?page=2"},"self":{"href":"/orders"}}}""" 
        "should return resource object with a _links object"   

    testCase "resource with properties to json" <| fun _ ->
      let resource = {
        links = Map.empty
        embedded = Embedded []
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
        embedded = Embedded []
        properties = Map.ofList [ "thing", someOject ]
      }
        
      Expect.equal 
        (resource |> serializeResource |> Json.format) 
        """{"thing":{"json":{"hello":"world"},"number":42,"string":"hello"}}""" 
        "should return resource object with property with json"                            
  ]