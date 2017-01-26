# F# JSON support for Hypertext Application Language (HAL) media type

This is a first, raw version.

Define a HAL resource representations in F# and convert it to JSON.

* Idiomatic support for F#
* Supports multiple serializers / formats (e.g. Newtonsoft.Json, Chiron, FSharp.Data.Json)
* Extendable with other formats

## Example (Newtonsoft.Json)

[Complete source script](https://github.com/battermann/halsharp/blob/master/Script.fsx)

Create instances of the response body models:

```fsharp
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
```

### Create embedded resources

Create empty resources and:

* add the record type instances as payload
* add links

```fsharp
let couponResource =
    Resource.empty
    |> withPayload coupon
    |> addLink "self" (Link.create (relUri "/api/members/109087/coupons/654"))

let shippingResource =
    Resource.empty
    |> withPayload shipping
    |> addLink "self" (Link.create (relUri "/api/members/109087/shippings/135451"))

let billingResource =
    Resource.empty
    |> withPayload billing
    |> addLink "self" (Link.create (relUri "/api/members/109087/billings/135451"))
```

### Create the main resource

Now the main resource can be created by adding the

* payload
* links
* embedded resources
* curies

```fsharp
let resource =
    Resource.empty
    |> withPayload payment
    |> addLink "self" (Link.create (relUri "/api/member/109087/payments/8888"))
    |> addLink "http://example.com/docs/rels/billing" (Link.create (relUri "/api/member/109087/billings/135451"))
    |> addLink "http://example.com/docs/rels/shipping" (Link.create (relUri "/api/member/109087/shipping/135451"))
    |> addLink "http://example.com/docs/rels/coupon" (Link.create (relUri "/api/member/109087/coupons/654"))
    |> addEmbedded "http://www.example.com/docs/rels/coupon" couponResource
    |> addEmbedded "http://www.example.com/docs/rels/shipping" shippingResource
    |> addEmbedded "http://www.example.com/docs/rels/billing" billingResource
    |> withCuries [ "ns", Uri "http://www.example.com/docs/rels/{rel}" ]
```

### Serialization

Serialize the resource with Newtonsoft.Json:

```fsharp
let serialize x = JsonConvert.SerializeObject(x, Formatting.Indented)

resource
|> ObjectInterpreter.toJson // returns obj
|> serialize
|> printfn "%A"
```

### Output

```json
{
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
}
```
