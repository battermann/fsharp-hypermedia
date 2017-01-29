#load "src/Hypermedia/Hypermedia.fs"
#load "paket-files/include-scripts/net40/include.newtonsoft.json.fsx"

open System
open Newtonsoft.Json
open Hypermedia
open Hal

let relUri path = Uri(path, UriKind.Relative)

type Payment = {
    subtotal: decimal
    tax: decimal
    freight: decimal
    total: decimal
}

type Coupon = {
    ``type``: string
    amount: decimal
    code: string
}

type Billing = {
    firstName: string
    lastName: string
    address: string
    city: string
    state: string
    zipcode: string
    countryIso: string
    cardNumber: string
    cardType: string
    cardExpYear: string
    cardExpMonth: string
}

type Shipping = {
    firstName: string
    lastName: string
    address: string
    city: string
    state: string
    zipcode: string
    countryIso: string    
}

let payment = {
    subtotal = 49m
    tax = 0m
    freight = 5m
    total = 44m
}

let coupon = {
    ``type`` = "dollarOff"
    amount = 10m
    code = "A0318A97"
}

let shipping = {
    firstName = "Heman"
    lastName = "Radtke"
    address = "1234 Day St."
    city = "Los Angeles"
    state = "CA"
    zipcode = "90015"
    countryIso = "US"
}

let billing = {
    firstName = "Heman"
    lastName = "Radtke"
    address = "1234 Day St."
    city = "Los Angeles"
    state = "CA"
    zipcode = "90015"
    countryIso = "US"
    cardNumber = "1111"
    cardType = "mastercard"
    cardExpYear = "2015"
    cardExpMonth = "01"
}

let couponResource =
    Resource.empty
    |> Resource.withPayload coupon
    |> Resource.addLink "self" (Link.create (relUri "/api/members/109087/coupons/654"))

let shippingResource =
    Resource.empty
    |> Resource.withPayload shipping
    |> Resource.addLink "self" (Link.create (relUri "/api/members/109087/shippings/135451"))

let billingResource =
    Resource.empty
    |> Resource.withPayload billing
    |> Resource.addLink "self" (Link.create (relUri "/api/members/109087/billings/135451"))

let resource = 
    Resource.empty
    |> Resource.withPayload payment
    |> Resource.addLink "self" (Link.create (relUri "/api/member/109087/payments/8888"))
    |> Resource.addLink "http://example.com/docs/rels/billing" (Link.create (relUri "/api/member/109087/billings/135451"))
    |> Resource.addLink "http://example.com/docs/rels/shipping" (Link.create (relUri "/api/member/109087/shipping/135451"))
    |> Resource.addLink "http://example.com/docs/rels/coupon" (Link.create (relUri "/api/member/109087/coupons/654"))
    |> Resource.addEmbedded "http://www.example.com/docs/rels/coupon" couponResource
    |> Resource.addEmbedded "http://www.example.com/docs/rels/shipping" shippingResource
    |> Resource.addEmbedded "http://www.example.com/docs/rels/billing" billingResource
    |> Resource.withCuries [ "ns", Uri "http://www.example.com/docs/rels/{rel}" ]

let serialize x = JsonConvert.SerializeObject(x, Formatting.Indented)

resource 
|> ObjectInterpreter.Hal.toJson 
|> serialize 
|> printfn "%A"

(*
"{
  "_embedded": {
    "ns:billing": {
      "_links": {
        "self": {
          "href": "/api/members/109087/billings/135451"
        }
      },
      "address": "1234 Day St.",
      "cardExpMonth": "01",
      "cardExpYear": "2015",
      "cardNumber": "1111",
      "cardType": "mastercard",
      "city": "Los Angeles",
      "countryIso": "US",
      "firstName": "Heman",
      "lastName": "Radtke",
      "state": "CA",
      "zipcode": "90015"
    },
    "ns:coupon": {
      "_links": {
        "self": {
          "href": "/api/members/109087/coupons/654"
        }
      },
      "amount": 10.0,
      "code": "A0318A97",
      "type": "dollarOff"
    },
    "ns:shipping": {
      "_links": {
        "self": {
          "href": "/api/members/109087/shippings/135451"
        }
      },
      "address": "1234 Day St.",
      "city": "Los Angeles",
      "countryIso": "US",
      "firstName": "Heman",
      "lastName": "Radtke",
      "state": "CA",
      "zipcode": "90015"
    }
  },
  "_links": {
    "curies": [
      {
        "href": "http://www.example.com/docs/rels/{rel}",
        "name": "ns",
        "templated": true
      }
    ],
    "ns:billing": {
      "href": "/api/member/109087/billings/135451"
    },
    "ns:coupon": {
      "href": "/api/member/109087/coupons/654"
    },
    "ns:shipping": {
      "href": "/api/member/109087/shipping/135451"
    },
    "self": {
      "href": "/api/member/109087/payments/8888"
    }
  },
  "freight": 5.0,
  "subtotal": 49.0,
  "tax": 0.0,
  "total": 44.0
}"    
*)