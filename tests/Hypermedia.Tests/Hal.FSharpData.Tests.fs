module Hal.FSharpData.Tests

open Expecto
open System

open Hal
open Hypermedia.Models

let relUri path = Uri(path, UriKind.Relative)

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
      links = Map.ofList [ "self", Singleton <| Link.create (relUri "/shipping/135451") ]
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
      links = Map.ofList [ "self", Singleton <| Link.create (relUri "/billing/135451") ]
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
      links = Map.ofList [ "self", Singleton <| Link.create (relUri "/payment")
                           "http://example.com/rels/billing", Singleton <| Link.create (relUri "/member/109087/billing")
                           "http://example.com/rels/shipping", Singleton <| Link.create (relUri "/member/109087/shipping")
                           "http://example.com/rels/payment/coupon", Singleton <| Link.create (relUri "/payment/coupon")
                           "http://example.com/rels/payment/billing", Singleton <| Link.create (relUri "/payment/billing")
                           "http://example.com/rels/payment/shipping", Singleton <| Link.create (relUri "/payment/shipping")
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