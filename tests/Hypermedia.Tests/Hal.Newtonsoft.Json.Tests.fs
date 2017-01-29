module Hal.NewtonsoftJson.Tests

open Expecto
open System

open Hypermedia
open Hal
open Newtonsoft.Json

let relUri path = Uri(path, UriKind.Relative)

type Person = {
  name: string
  age: int
}

[<Tests>]
let ``Json.NET`` =
  testList "json.net" [
    testCase "empty resource" <| fun _ ->
      Expect.equal 
        (Resource.empty |> ObjectInterpreter.Hal.toJson |> JsonConvert.SerializeObject) 
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
          links = Map.ofList [ "self", Singleton <| Link.create (relUri "/shipping/135451") ]
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
          links = Map.ofList [ "self", Singleton <| Link.create (relUri "/billing/135451") ]
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
          links = Map.ofList [ "self", Singleton <| Link.create (relUri "/payment")
                               "http://example.com/rels/billing", Singleton <| Link.create (relUri "/member/109087/billing")
                               "http://example.com/rels/shipping", Singleton <|Link.create (relUri "/member/109087/shipping")
                               "http://example.com/rels/payment/coupon", Singleton <|Link.create (relUri "/payment/coupon")
                               "http://example.com/rels/payment/billing", Singleton <|Link.create (relUri "/payment/billing")
                               "http://example.com/rels/payment/shipping", Singleton <|Link.create (relUri "/payment/shipping")
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
        (eCommerceResource |> ObjectInterpreter.Hal.toJson |> JsonConvert.SerializeObject) 
        """{"_embedded":{"http://example.com/rels/billing":{"_links":{"self":{"href":"/billing/135451"}},"address":"1234 Day St.","card_exp_month":"01","card_exp_year":"2015","card_number":"1111","card_type":"mastercard","city":"Los Angeles","country_iso":"US","first_name":"Herman","last_name":"Radtke","state":"CA","zipcode":"90015"},"http://example.com/rels/shipping":{"_links":{"self":{"href":"/shipping/135451"}},"address":"1234 Day St.","city":"Los Angeles","country_iso":"US","first_name":"Heman","last_name":"Radtke","state":"CA","zipcode":"90015"},"http://www.example.com/rels/coupon":{"amount":"10","code":"A0318A97","type":"dollarOff"}},"_links":{"http://example.com/rels/billing":{"href":"/member/109087/billing"},"http://example.com/rels/payment/billing":{"href":"/payment/billing"},"http://example.com/rels/payment/coupon":{"href":"/payment/coupon"},"http://example.com/rels/payment/shipping":{"href":"/payment/shipping"},"http://example.com/rels/shipping":{"href":"/member/109087/shipping"},"self":{"href":"/payment"}},"freight":5.0,"subtotal":49.0,"tax":0.0,"total":44.0}""" 
        "should return resource object corresponding to correct e-commerce example"
    testCase "resource with payload" <| fun _ ->
      let resource = {
          Resource.empty with
            payload = { name = "John"; age = 42 } :> obj |> Some
        }    
      Expect.equal
        (resource |> ObjectInterpreter.Hal.toJson |> JsonConvert.SerializeObject) 
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
        (resourceWithEmbedded |> ObjectInterpreter.Hal.toJson |> JsonConvert.SerializeObject) 
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
        (resource |> ObjectInterpreter.Hal.toJson |> JsonConvert.SerializeObject) 
        """{"age":42,"foo":"bar","name":"John","total":42.0}""" 
        "should return an empty resource"          
    testCase "resource with invalid payload" <| fun _ ->
      let resource = {
          Resource.empty with
            payload = [1;2;3] :> obj |> Some
        }    
      Expect.equal
        (resource |> ObjectInterpreter.Hal.toJson |> JsonConvert.SerializeObject) 
        """{}""" 
        "should return an empty resource"                
  ]