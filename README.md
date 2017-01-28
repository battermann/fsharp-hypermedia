# F# JSON support for HAL and Siren hypermedia types

This is a first, raw version.

Define [HAL resource](http://stateless.co/hal_specification.html) or [Siren](https://github.com/kevinswiber/siren) representations in F# and convert them to JSON.

* Idiomatic support for F#
* Supports multiple serializers / formats (e.g. Newtonsoft.Json (obj), Chiron.Json, FSharp.Data.JsonValue)
* Extendable with other formats
* Mediatypes
  * `application/hal+json` ([spec](http://stateless.co/hal_specification.html))
  * `application/vnd.siren+json` ([spec](https://github.com/kevinswiber/siren))

## TOC

* [HAL Example with Newtonsoft.Json](https://github.com/battermann/fsharp-hypermedia#hal-example-with-newtonsoftjson)
* [Siren Example with FSharp.Data](https://github.com/battermann/fsharp-hypermedia#siren-example-with-fsharpdata)

## HAL Example with Newtonsoft.Json

[Complete source script](https://github.com/battermann/halsharp/blob/master/Script.fsx)

Create instances of the response body models:

```fsharp
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
```

### Create embedded resources

Create empty resources and:

* add the record type instances as payload
* add links

```fsharp
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
    |> Resource.withPayload payment
    |> Resource.addLink "self" (Link.create (relUri "/api/member/109087/payments/8888"))
    |> Resource.addLink "http://example.com/docs/rels/billing" (Link.create (relUri "/api/member/109087/billings/135451"))
    |> Resource.addLink "http://example.com/docs/rels/shipping" (Link.create (relUri "/api/member/109087/shipping/135451"))
    |> Resource.addLink "http://example.com/docs/rels/coupon" (Link.create (relUri "/api/member/109087/coupons/654"))
    |> Resource.addEmbedded "http://www.example.com/docs/rels/coupon" couponResource
    |> Resource.addEmbedded "http://www.example.com/docs/rels/shipping" shippingResource
    |> Resource.addEmbedded "http://www.example.com/docs/rels/billing" billingResource
    |> Resource.withCuries [ "ns", Uri "http://www.example.com/docs/rels/{rel}" ]
```

### Serialization

Serialize the resource with Newtonsoft.Json:

```fsharp
let serialize x = JsonConvert.SerializeObject(x, Formatting.Indented)

resource
|> ObjectInterpreter.Hal.toJson // returns obj
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

## Siren Example with FSharp.Data

[Complete source script](https://github.com/battermann/fsharp-hypermedia/blob/master/SirenExamples.fsx)

Define embedded link:

```fsharp
let itemsLink = 
    Link.create (Rel "http://x.io/rels/order-items") (Href (Uri "http://api.x.io/orders/42/items"))
    |> Link.withClasses [ "collection"; "items" ]
```

Define an embedded entity:

```fsharp
let customer =
    Entity.empty
    |> Entity.withClasses [ "customer"; "info" ]
    |> Entity.addProperty "customerId" (JsonValue.String "pj123")
    |> Entity.addProperty "name" (JsonValue.String "Peter Joseph")
    |> Entity.withLinks [ Link.create (Rel "self") (Href (Uri "http://api.x.io/customers/pj123")) ]
```

Define an action:

```fsharp
let addItemAction = {
    Action.create (Href (Uri "http://api.x.io/orders/42/items")) with
        title = Some (Title "Add Item")
        httpMethod = Some HttpMethod.POST
        mediaType = Some (MediaType "application/x-www-form-urlencoded")
        fields = Map.ofList [ Name "orderNumber", { Field.empty with inputType = Some InputType.Hidden; value = Some (Value "42") }
                              Name "productCode", { Field.empty with inputType = Some InputType.Text }
                              Name "quantity", { Field.empty with inputType = Some InputType.Number } ]
}
```

Build the main entity:

```fsharp
let entity =
    Entity.empty
    |> Entity.addProperty "orderNumber" (JsonValue.Number 42m)
    |> Entity.addProperty "itemCount" (JsonValue.Number 3m)
    |> Entity.addProperty "status" (JsonValue.String "pending")
    |> Entity.withClasses [ "order" ]       
    |> Entity.addEmbeddedLink itemsLink
    |> Entity.addEmbeddedEntity customer "http://x.io/rels/customer"
    |> Entity.withActions [ "add-item", addItemAction ] 
    |> Entity.withLinks [ Link.create (Rel "self") (Href (Uri "http://api.x.io/orders/42"))
                          Link.create (Rel "previous") (Href (Uri "http://api.x.io/orders/41"))
                          Link.create (Rel "next") (Href (Uri "http://api.x.io/orders/43")) ]
```

Serialization:

```fsharp
FSharpDataIntepreter.Siren.toJSon entity
|> fun x -> x.ToString()
|> printfn "%A"
```

Output:

```json
{
  "actions": [
    {
      "fields": [
        {
          "name": "orderNumber",
          "type": "hidden",
          "value": "42"
        },
        {
          "name": "productCode",
          "type": "text"
        },
        {
          "name": "quantity",
          "type": "number"
        }
      ],
      "href": "http://api.x.io/orders/42/items",
      "method": "POST",
      "name": "add-item",
      "title": "Add Item",
      "type": "application/x-www-form-urlencoded"
    }
  ],
  "class": [
    "order"
  ],
  "entities": [
    {
      "class": [
        "customer",
        "info"
      ],
      "links": [
        {
          "href": "http://api.x.io/customers/pj123",
          "rel": [
            "self"
          ]
        }
      ],
      "properties": {
        "customerId": "pj123",
        "name": "Peter Joseph"
      },
      "rel": [
        "http://x.io/rels/customer"
      ]
    },
    {
      "class": [
        "collection",
        "items"
      ],
      "href": "http://api.x.io/orders/42/items",
      "rel": [
        "http://x.io/rels/order-items"
      ]
    }
  ],
  "links": [
    {
      "href": "http://api.x.io/orders/42",
      "rel": [
        "self"
      ]
    },
    {
      "href": "http://api.x.io/orders/41",
      "rel": [
        "previous"
      ]
    },
    {
      "href": "http://api.x.io/orders/43",
      "rel": [
        "next"
      ]
    }
  ],
  "properties": {
    "itemCount": 3,
    "orderNumber": 42,
    "status": "pending"
  }
}
```
