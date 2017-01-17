module Tests

open Expecto
open Chiron

open HalSharp

open Types

[<Tests>]
let tests =

  testList "links" [
    testCase "simple link to json" <| fun _ ->
      Expect.equal (Links.toString (Links.simpleLink "/orders")) """{"href":"/orders"}""" "should return simple link with href attribute"
    testCase "link with template to json" <| fun _ ->
      Expect.equal (Links.toString { Links.simpleLink "/orders/{id}" with templated = Some true }) """{"href":"/orders/{id}","templated":true}""" "should return link object with href and templated attribute"      
  ]