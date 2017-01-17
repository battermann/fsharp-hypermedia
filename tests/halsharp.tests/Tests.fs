module Tests

open Expecto
open Chiron

open HalSharp

open Types

open Resources

[<Tests>]
let ``Link tests`` =

  testList "links" [
    testCase "simple link to string" <| fun _ ->
      Expect.equal (Links.simpleLink "/orders" |> Links.serializeLink |> Json.format) """{"href":"/orders"}""" "should return simple link with href attribute"
    testCase "link with template to string" <| fun _ ->
      Expect.equal ({ Links.simpleLink "/orders/{id}" with templated = Some true } |> Links.serializeLink |> Json.format) """{"href":"/orders/{id}","templated":true}""" "should return link object with href and templated attribute"
  ]


[<Tests>]
let ``Resource tests`` =
  testList "resources" [   
    testCase "link with template to json" <| fun _ ->
      let resource = {
        links = Map.ofList [ "self", Links.simpleLink "/orders"
                             "next", Links.simpleLink "/orders?page=2"
                           ]
        embedded = Embedded []
        properties = Map.ofList []
      }
        
      Expect.equal (resource |> serializeResource |> Json.format) """{"_links":{"next":{"href":"/orders?page=2"},"self":{"href":"/orders"}}}""" "should return resource object with a _links object"            
  ]